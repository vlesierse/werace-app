using System.Text.RegularExpressions;

namespace WeRace.DataImport.Importers;

/// <summary>
/// Maps Jolpica/Ergast table and column names to WeRace PostgreSQL schema.
/// Handles camelCase to snake_case conversion and the races.year → races.season_id FK resolution.
/// </summary>
public static partial class SchemaMapper
{
    /// <summary>
    /// Jolpica table name → WeRace table name.
    /// Most are identity or camelCase → snake_case.
    /// </summary>
    private static readonly Dictionary<string, string> TableMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["seasons"] = "seasons",
        ["circuits"] = "circuits",
        ["races"] = "races",
        ["drivers"] = "drivers",
        ["constructors"] = "constructors",
        ["status"] = "status",
        ["results"] = "results",
        ["qualifying"] = "qualifying",
        ["sprintResults"] = "sprint_results",
        ["sprint_results"] = "sprint_results",
        ["pitStops"] = "pit_stops",
        ["pit_stops"] = "pit_stops",
        ["lapTimes"] = "lap_times",
        ["lap_times"] = "lap_times",
        ["driverStandings"] = "driver_standings",
        ["driver_standings"] = "driver_standings",
        ["constructorStandings"] = "constructor_standings",
        ["constructor_standings"] = "constructor_standings",
        ["constructorResults"] = "constructor_results",
        ["constructor_results"] = "constructor_results",
    };

    /// <summary>
    /// Per-table column mappings. Key: (werace_table, jolpica_column_index) → werace_column_name.
    /// These define the column order for PostgreSQL COPY operations.
    /// </summary>
    private static readonly Dictionary<string, string[]> ColumnOrder = new()
    {
        ["seasons"] = ["id", "year", "wikipedia_url"],
        ["circuits"] = ["id", "circuit_ref", "name", "location", "country", "latitude", "longitude", "altitude", "wikipedia_url"],
        ["races"] = ["id", "season_id", "round", "name", "circuit_id", "date", "time", "wikipedia_url", "fp1_date", "fp1_time", "fp2_date", "fp2_time", "fp3_date", "fp3_time", "quali_date", "quali_time", "sprint_date", "sprint_time"],
        ["drivers"] = ["id", "driver_ref", "number", "code", "forename", "surname", "date_of_birth", "nationality", "wikipedia_url"],
        ["constructors"] = ["id", "constructor_ref", "name", "nationality", "wikipedia_url"],
        ["status"] = ["id", "status"],
        ["results"] = ["id", "race_id", "driver_id", "constructor_id", "number", "grid", "position", "position_text", "position_order", "points", "laps", "time", "milliseconds", "fastest_lap", "rank", "fastest_lap_time", "fastest_lap_speed", "status_id"],
        ["qualifying"] = ["id", "race_id", "driver_id", "constructor_id", "number", "position", "q1", "q2", "q3"],
        ["sprint_results"] = ["id", "race_id", "driver_id", "constructor_id", "number", "grid", "position", "position_text", "position_order", "points", "laps", "time", "milliseconds", "fastest_lap", "fastest_lap_time", "status_id"],
        ["pit_stops"] = ["race_id", "driver_id", "stop", "lap", "time", "duration", "milliseconds"],
        ["lap_times"] = ["race_id", "driver_id", "lap", "position", "time", "milliseconds"],
        ["driver_standings"] = ["id", "race_id", "driver_id", "points", "position", "position_text", "wins"],
        ["constructor_standings"] = ["id", "race_id", "constructor_id", "points", "position", "position_text", "wins"],
        ["constructor_results"] = ["id", "race_id", "constructor_id", "points", "status"],
    };

    /// <summary>
    /// Maps a Jolpica table name to the WeRace table name. Returns null if unmapped.
    /// </summary>
    public static string? MapTableName(string jolpicaTable)
    {
        return TableMap.TryGetValue(jolpicaTable, out var mapped) ? mapped : null;
    }

    /// <summary>
    /// Returns the column names in order for a WeRace table (used for COPY column list).
    /// </summary>
    public static string[]? GetColumnOrder(string weRaceTable)
    {
        return ColumnOrder.TryGetValue(weRaceTable, out var cols) ? cols : null;
    }

    /// <summary>
    /// Converts a camelCase or PascalCase column name to snake_case.
    /// </summary>
    [GeneratedRegex(@"(?<=[a-z0-9])([A-Z])")]
    private static partial Regex CamelCaseRegex();

    public static string ToSnakeCase(string name)
    {
        return CamelCaseRegex().Replace(name, "_$1").ToLowerInvariant();
    }

    /// <summary>
    /// Maps all parsed Jolpica tables to WeRace table format.
    /// Resolves races.year → races.season_id using the seasons lookup.
    /// </summary>
    public static Dictionary<string, List<string[]>> MapAll(
        Dictionary<string, List<string[]>> jolpicaData)
    {
        var result = new Dictionary<string, List<string[]>>();

        // Build year → seasonId lookup from the seasons table
        var yearToSeasonId = new Dictionary<string, string>();
        if (jolpicaData.TryGetValue("seasons", out var seasonRows))
        {
            foreach (var row in seasonRows)
            {
                if (row.Length >= 2)
                {
                    // seasons: [id, year, url]
                    yearToSeasonId[row[1]] = row[0]; // year → id
                }
            }
        }

        foreach (var (jolpicaTable, rows) in jolpicaData)
        {
            var weRaceTable = MapTableName(jolpicaTable);
            if (weRaceTable == null) continue;

            var mappedRows = new List<string[]>(rows.Count);

            foreach (var row in rows)
            {
                var mappedRow = MapRow(weRaceTable, jolpicaTable, row, yearToSeasonId);
                if (mappedRow != null)
                {
                    mappedRows.Add(mappedRow);
                }
            }

            if (result.TryGetValue(weRaceTable, out var existing))
            {
                existing.AddRange(mappedRows);
            }
            else
            {
                result[weRaceTable] = mappedRows;
            }
        }

        return result;
    }

    /// <summary>
    /// Maps a single Jolpica row to WeRace format.
    /// The races table needs special handling: column index 1 is year in Jolpica, season_id in WeRace.
    /// </summary>
    private static string[]? MapRow(
        string weRaceTable,
        string jolpicaTable,
        string[] row,
        Dictionary<string, string> yearToSeasonId)
    {
        if (weRaceTable == "races" && string.Equals(jolpicaTable, "races", StringComparison.OrdinalIgnoreCase))
        {
            // Jolpica races: [raceId, year, round, circuitId, name, date, time, url, fp1_date, fp1_time, ...]
            // WeRace races:  [id, season_id, round, name, circuit_id, date, time, url, fp1_date, fp1_time, ...]
            if (row.Length < 8) return null;

            var year = row[1];
            if (!yearToSeasonId.TryGetValue(year, out var seasonId))
            {
                // Unknown season — skip this race
                return null;
            }

            // Reorder: [id, season_id, round, name, circuit_id, date, time, url, fp1_date, ...]
            var mapped = new string[row.Length];
            mapped[0] = row[0];     // id
            mapped[1] = seasonId;   // year → season_id
            mapped[2] = row[2];     // round
            mapped[3] = row[4];     // name (index 4 in Jolpica)
            mapped[4] = row[3];     // circuit_id (index 3 in Jolpica)
            mapped[5] = row[5];     // date
            mapped[6] = row[6];     // time
            mapped[7] = row[7];     // wikipedia_url

            // Copy remaining session dates/times if present
            for (var i = 8; i < row.Length; i++)
            {
                mapped[i] = row[i];
            }

            return mapped;
        }

        // All other tables: pass through as-is (column order matches)
        return row;
    }

    /// <summary>
    /// Converts a SQL value literal to a CLR-friendly string for COPY.
    /// Handles NULL, \N, empty strings, and MySQL zero dates.
    /// </summary>
    public static string? NormalizeValue(string value)
    {
        if (string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "\\N", StringComparison.Ordinal))
        {
            return null;
        }

        // MySQL zero date
        if (value is "0000-00-00" or "00:00:00")
        {
            return null;
        }

        return value;
    }
}
