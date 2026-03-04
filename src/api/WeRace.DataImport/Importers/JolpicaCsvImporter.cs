using System.Diagnostics;
using Npgsql;

namespace WeRace.DataImport.Importers;

/// <summary>
/// Import mode: full (truncate + reload) or delta (upsert via temp table).
/// </summary>
public enum ImportMode
{
    Full,
    Delta
}

/// <summary>
/// Orchestrates the Jolpica CSV → PostgreSQL import pipeline.
/// Parses the CSV directory, maps to WeRace schema, bulk loads via COPY, and validates.
/// </summary>
public class JolpicaCsvImporter(string connectionString)
{
    /// <summary>
    /// Table load order respecting FK dependencies.
    /// Parent tables first, child tables after.
    /// </summary>
    private static readonly string[] LoadOrder =
    [
        "seasons",
        "circuits",
        "status",
        "drivers",
        "constructors",
        "races",
        "results",
        "qualifying",
        "sprint_results",
        "pit_stops",
        "lap_times",
        "driver_standings",
        "constructor_standings",
        "constructor_results"
    ];

    /// <summary>
    /// Reverse order for truncation (children first to avoid FK violations).
    /// </summary>
    private static readonly string[] TruncateOrder = LoadOrder.Reverse().ToArray();

    public async Task ImportAsync(string csvDirectoryPath, ImportMode mode)
    {
        var totalSw = Stopwatch.StartNew();

        // Step 1: Parse CSV directory
        Console.WriteLine("Parsing CSV files...");
        var parseSw = Stopwatch.StartNew();
        var csvData = CsvDataParser.Parse(csvDirectoryPath);
        parseSw.Stop();

        Console.WriteLine($"  Parsed {csvData.Count} CSV files in {parseSw.Elapsed.TotalSeconds:F1}s");
        foreach (var (table, data) in csvData.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"    {table}: {data.Rows.Count:N0} rows ({data.Headers.Length} columns)");
        }
        Console.WriteLine();

        // Step 2: Map to WeRace schema
        Console.WriteLine("Mapping to WeRace schema...");
        var mapped = SchemaMapper.MapAll(csvData);
        Console.WriteLine($"  Mapped to {mapped.Count} target tables");
        foreach (var (table, rows) in mapped.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"    {table}: {rows.Count:N0} rows");
        }
        Console.WriteLine();

        // Step 3: Load into PostgreSQL
        await using var dataSource = NpgsqlDataSource.Create(connectionString);

        if (mode == ImportMode.Full)
        {
            await TruncateAllAsync(dataSource);
        }

        Console.WriteLine($"Loading data ({mode} mode)...");
        var stats = new Dictionary<string, (int Rows, TimeSpan Elapsed)>();

        foreach (var table in LoadOrder)
        {
            if (!mapped.TryGetValue(table, out var rows) || rows.Count == 0)
            {
                Console.WriteLine($"  {table}: skipped (no data)");
                continue;
            }

            var columns = SchemaMapper.GetColumnOrder(table);
            if (columns == null)
            {
                Console.WriteLine($"  {table}: skipped (no column mapping)");
                continue;
            }

            var tableSw = Stopwatch.StartNew();

            if (mode == ImportMode.Full)
            {
                await BulkCopyAsync(dataSource, table, columns, rows);
            }
            else
            {
                await UpsertAsync(dataSource, table, columns, rows);
            }

            tableSw.Stop();
            stats[table] = (rows.Count, tableSw.Elapsed);
            Console.WriteLine($"  {table}: {rows.Count:N0} rows ({tableSw.Elapsed.TotalSeconds:F1}s)");
        }

        // Step 4: Reset sequences for SERIAL columns after COPY
        if (mode == ImportMode.Full)
        {
            Console.WriteLine();
            Console.WriteLine("Resetting sequences...");
            await ResetSequencesAsync(dataSource);
        }

        // Step 5: Validate
        Console.WriteLine();
        Console.WriteLine("Running post-import validation...");
        var validator = new DataValidator(connectionString);
        await validator.ValidateAsync();

        totalSw.Stop();
        Console.WriteLine();
        Console.WriteLine($"Import complete in {totalSw.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine();
        Console.WriteLine("Summary:");
        foreach (var (table, (rows, elapsed)) in stats.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"  {table,-30} {rows,10:N0} rows  {elapsed.TotalSeconds,6:F1}s");
        }
    }

    private static async Task TruncateAllAsync(NpgsqlDataSource dataSource)
    {
        Console.WriteLine("Truncating all tables...");
        await using var conn = await dataSource.OpenConnectionAsync();

        foreach (var table in TruncateOrder)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"TRUNCATE TABLE {table} CASCADE";
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine("  Done.");
        Console.WriteLine();
    }

    /// <summary>
    /// Bulk load rows into a table using PostgreSQL COPY with text format.
    /// </summary>
    private static async Task BulkCopyAsync(
        NpgsqlDataSource dataSource,
        string table,
        string[] columns,
        List<string[]> rows)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        var columnList = string.Join(", ", columns);
        var copyCommand = $"COPY {table} ({columnList}) FROM STDIN (FORMAT text, NULL '\\N')";

        await using var writer = await conn.BeginTextImportAsync(copyCommand);

        foreach (var row in rows)
        {
            var values = new string[columns.Length];
            for (var i = 0; i < columns.Length; i++)
            {
                if (i < row.Length)
                {
                    var normalized = SchemaMapper.NormalizeValue(row[i]);
                    values[i] = normalized ?? "\\N";
                }
                else
                {
                    values[i] = "\\N";
                }
            }

            await writer.WriteLineAsync(string.Join('\t', values));
        }
    }

    /// <summary>
    /// Upsert rows using a temp table + INSERT ON CONFLICT DO UPDATE.
    /// </summary>
    private static async Task UpsertAsync(
        NpgsqlDataSource dataSource,
        string table,
        string[] columns,
        List<string[]> rows)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        var pkColumns = GetPrimaryKeyColumns(table);

        // Create temp table
        var tempTable = $"_tmp_{table}";
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"CREATE TEMP TABLE {tempTable} (LIKE {table} INCLUDING ALL) ON COMMIT DROP";
            await cmd.ExecuteNonQueryAsync();
        }

        // COPY into temp table
        var columnList = string.Join(", ", columns);
        var copyCommand = $"COPY {tempTable} ({columnList}) FROM STDIN (FORMAT text, NULL '\\N')";
        await using (var writer = await conn.BeginTextImportAsync(copyCommand))
        {
            foreach (var row in rows)
            {
                var values = new string[columns.Length];
                for (var i = 0; i < columns.Length; i++)
                {
                    if (i < row.Length)
                    {
                        var normalized = SchemaMapper.NormalizeValue(row[i]);
                        values[i] = normalized ?? "\\N";
                    }
                    else
                    {
                        values[i] = "\\N";
                    }
                }

                await writer.WriteLineAsync(string.Join('\t', values));
            }
        }

        // Upsert from temp into main
        var updateSet = string.Join(", ", columns.Where(c => !pkColumns.Contains(c)).Select(c => $"{c} = EXCLUDED.{c}"));
        var conflict = string.Join(", ", pkColumns);

        string upsertSql;
        if (string.IsNullOrEmpty(updateSet))
        {
            upsertSql = $"INSERT INTO {table} ({columnList}) SELECT {columnList} FROM {tempTable} ON CONFLICT ({conflict}) DO NOTHING";
        }
        else
        {
            upsertSql = $"INSERT INTO {table} ({columnList}) SELECT {columnList} FROM {tempTable} ON CONFLICT ({conflict}) DO UPDATE SET {updateSet}";
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = upsertSql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string[] GetPrimaryKeyColumns(string table)
    {
        return table switch
        {
            "pit_stops" => ["race_id", "driver_id", "stop"],
            "lap_times" => ["race_id", "driver_id", "lap"],
            _ => ["id"]
        };
    }

    /// <summary>
    /// Resets SERIAL sequences to max(id) + 1 after COPY.
    /// </summary>
    private static async Task ResetSequencesAsync(NpgsqlDataSource dataSource)
    {
        string[] tablesWithSerial =
        [
            "seasons", "circuits", "races", "drivers", "constructors", "status",
            "results", "qualifying", "sprint_results",
            "driver_standings", "constructor_standings", "constructor_results"
        ];

        await using var conn = await dataSource.OpenConnectionAsync();

        foreach (var table in tablesWithSerial)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT setval(pg_get_serial_sequence('{table}', 'id'), COALESCE(MAX(id), 1)) FROM {table}";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
