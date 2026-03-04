using Npgsql;

namespace WeRace.DataImport.Importers;

/// <summary>
/// Post-import validation: row counts, FK integrity, data range, and spot checks.
/// </summary>
public class DataValidator(string connectionString)
{
    public async Task ValidateAsync()
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var conn = await dataSource.OpenConnectionAsync();

        var passed = true;

        passed &= await CheckRowCounts(conn);
        passed &= await CheckForeignKeyIntegrity(conn);
        passed &= await CheckDataRange(conn);
        passed &= await CheckKnownDataPoints(conn);

        Console.WriteLine();
        Console.WriteLine(passed
            ? "Validation: ALL CHECKS PASSED"
            : "Validation: SOME CHECKS FAILED — review output above");
    }

    private static async Task<bool> CheckRowCounts(NpgsqlConnection conn)
    {
        Console.WriteLine("  Row counts:");

        string[] tables =
        [
            "seasons", "circuits", "races", "drivers", "constructors", "status",
            "results", "qualifying", "sprint_results",
            "pit_stops", "lap_times",
            "driver_standings", "constructor_standings", "constructor_results"
        ];

        var allPopulated = true;

        foreach (var table in tables)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
            var count = (long)(await cmd.ExecuteScalarAsync())!;
            Console.WriteLine($"    {table,-30} {count,10:N0}");

            // Parent tables must have data; child tables may be empty for older dumps
            if (table is "seasons" or "circuits" or "races" or "drivers" or "constructors" && count == 0)
            {
                Console.WriteLine($"    WARNING: {table} has zero rows — import may have failed");
                allPopulated = false;
            }
        }

        return allPopulated;
    }

    private static async Task<bool> CheckForeignKeyIntegrity(NpgsqlConnection conn)
    {
        Console.WriteLine("  FK integrity:");
        var passed = true;

        // Orphaned results (results referencing nonexistent races)
        passed &= await CheckOrphans(conn, "results", "races", "race_id", "id");
        passed &= await CheckOrphans(conn, "results", "drivers", "driver_id", "id");
        passed &= await CheckOrphans(conn, "results", "constructors", "constructor_id", "id");
        passed &= await CheckOrphans(conn, "results", "status", "status_id", "id");

        // Orphaned standings
        passed &= await CheckOrphans(conn, "driver_standings", "races", "race_id", "id");
        passed &= await CheckOrphans(conn, "driver_standings", "drivers", "driver_id", "id");
        passed &= await CheckOrphans(conn, "constructor_standings", "races", "race_id", "id");
        passed &= await CheckOrphans(conn, "constructor_standings", "constructors", "constructor_id", "id");

        // Orphaned races (races referencing nonexistent seasons/circuits)
        passed &= await CheckOrphans(conn, "races", "seasons", "season_id", "id");
        passed &= await CheckOrphans(conn, "races", "circuits", "circuit_id", "id");

        return passed;
    }

    private static async Task<bool> CheckOrphans(
        NpgsqlConnection conn,
        string childTable,
        string parentTable,
        string fkColumn,
        string pkColumn)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT COUNT(*) FROM {childTable} c
            LEFT JOIN {parentTable} p ON c.{fkColumn} = p.{pkColumn}
            WHERE p.{pkColumn} IS NULL
            """;

        var orphans = (long)(await cmd.ExecuteScalarAsync())!;

        if (orphans > 0)
        {
            Console.WriteLine($"    FAIL: {orphans:N0} orphaned rows in {childTable}.{fkColumn} → {parentTable}.{pkColumn}");
            return false;
        }

        Console.WriteLine($"    OK: {childTable}.{fkColumn} → {parentTable}.{pkColumn}");
        return true;
    }

    private static async Task<bool> CheckDataRange(NpgsqlConnection conn)
    {
        Console.WriteLine("  Data range:");

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT MIN(year) AS earliest, MAX(year) AS latest, COUNT(DISTINCT year) AS total
            FROM seasons
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var earliest = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var latest = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var total = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

            Console.WriteLine($"    Seasons: {earliest}–{latest} ({total} seasons)");

            if (earliest > 1950)
            {
                Console.WriteLine($"    WARNING: Earliest season is {earliest}, expected 1950");
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> CheckKnownDataPoints(NpgsqlConnection conn)
    {
        Console.WriteLine("  Spot checks:");

        // 2023 Bahrain GP winner should be Verstappen
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT d.surname
            FROM results res
            JOIN races r ON res.race_id = r.id
            JOIN seasons s ON r.season_id = s.id
            JOIN drivers d ON res.driver_id = d.id
            WHERE s.year = 2023 AND r.round = 1 AND res.position = 1
            """;

        var winner = (string?)await cmd.ExecuteScalarAsync();

        if (winner == null)
        {
            Console.WriteLine("    SKIP: 2023 Bahrain GP data not found (may not be in dump)");
            return true; // Not a failure — dump may not include 2023
        }

        if (string.Equals(winner, "Verstappen", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("    OK: 2023 Bahrain GP winner = Verstappen");
            return true;
        }

        Console.WriteLine($"    FAIL: 2023 Bahrain GP winner = {winner}, expected Verstappen");
        return false;
    }
}
