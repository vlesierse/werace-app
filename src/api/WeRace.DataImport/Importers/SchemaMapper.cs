namespace WeRace.DataImport.Importers;

/// <summary>
/// Maps Jolpica CSV data to WeRace PostgreSQL schema.
///
/// The Jolpica CSV model uses a normalized structure:
///   session → round, sessionentry → roundentry → teamdriver
/// while the WeRace (Ergast-compatible) schema uses denormalized tables:
///   results (race_id, driver_id, constructor_id, ...).
///
/// This mapper resolves FK chains and transforms the data model accordingly.
/// </summary>
public static class SchemaMapper
{
    /// <summary>
    /// Column order for PostgreSQL COPY operations per WeRace table.
    /// Must match the schema column order exactly.
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
    /// Session types that represent qualifying phases.
    /// Q1/Q2/Q3 = modern three-phase qualifying.
    /// QA = qualifying aggregate, QB = qualifying battle (pre-2003), QO = one-shot qualifying.
    /// </summary>
    private static readonly HashSet<string> QualifyingTypes = ["Q1", "Q2", "Q3", "QA", "QB", "QO"];

    /// <summary>
    /// Session types that represent sprint race.
    /// </summary>
    private static readonly HashSet<string> SprintRaceTypes = ["SR"];

    /// <summary>
    /// Returns the COPY column order for a WeRace table.
    /// </summary>
    public static string[]? GetColumnOrder(string weRaceTable)
    {
        return ColumnOrder.TryGetValue(weRaceTable, out var cols) ? cols : null;
    }

    /// <summary>
    /// Normalizes a CSV value for PostgreSQL COPY ingestion.
    /// Empty strings, "NULL", "\N" → null (will become \N in COPY output).
    /// </summary>
    public static string? NormalizeValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase)) return null;
        if (string.Equals(value, "\\N", StringComparison.Ordinal)) return null;
        return value;
    }

    /// <summary>
    /// Maps all parsed Jolpica CSV tables to WeRace PostgreSQL format.
    /// Returns a dictionary keyed by WeRace table name with rows ordered per GetColumnOrder.
    /// </summary>
    public static Dictionary<string, List<string[]>> MapAll(Dictionary<string, CsvTable> csvData)
    {
        var result = new Dictionary<string, List<string[]>>();

        // Build lookup dictionaries for FK chain resolution
        var sessionById = BuildRowLookup(csvData, "session", "id");
        var roundEntryById = BuildRowLookup(csvData, "roundentry", "id");
        var teamDriverById = BuildRowLookup(csvData, "teamdriver", "id");
        var lapById = BuildRowLookup(csvData, "lap", "id");
        var sessionsByRound = BuildGroupLookup(csvData, "session", "round_id");

        // 1:1 table mappings
        MapSeasons(csvData, result);
        MapCircuits(csvData, result);
        MapDrivers(csvData, result);
        MapConstructors(csvData, result);

        // Races: merge round + session data for time/session dates
        MapRaces(csvData, result, sessionsByRound);

        // Status table: derived from distinct sessionentry.detail values
        var statusMap = BuildAndMapStatus(csvData, result);

        // Session entries → results / qualifying / sprint_results via FK chain
        MapSessionEntries(csvData, result, sessionById, roundEntryById, teamDriverById, statusMap);

        // Championship standings
        MapDriverStandings(csvData, result, sessionById);
        MapConstructorStandings(csvData, result, sessionById);

        // Lap times and pit stops (FK chain through session entries)
        var sessionEntryCtx = BuildSessionEntryLookup(csvData, sessionById, roundEntryById, teamDriverById);
        MapLapTimes(csvData, result, sessionEntryCtx);
        MapPitStops(csvData, result, sessionEntryCtx, lapById);

        // constructor_results: no direct CSV source
        // TODO: Derive from results by aggregating constructor points per race
        if (!result.ContainsKey("constructor_results"))
        {
            Console.WriteLine("  WARNING: constructor_results table has no CSV source — skipped");
            result["constructor_results"] = [];
        }

        return result;
    }

    // ── Lookup Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Builds a dictionary mapping a key column's value to the entire row (as column→value dictionary).
    /// Last row wins if keys are duplicated.
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> BuildRowLookup(
        Dictionary<string, CsvTable> csvData, string tableName, string keyColumn)
    {
        if (!csvData.TryGetValue(tableName, out var table))
            return new();

        var keyIdx = Array.IndexOf(table.Headers, keyColumn);
        if (keyIdx < 0) return new();

        var lookup = new Dictionary<string, Dictionary<string, string>>();
        foreach (var row in table.Rows)
        {
            var key = keyIdx < row.Length ? row[keyIdx] : "";
            if (string.IsNullOrEmpty(key)) continue;

            var dict = new Dictionary<string, string>();
            for (var i = 0; i < table.Headers.Length && i < row.Length; i++)
                dict[table.Headers[i]] = row[i];

            lookup[key] = dict;
        }
        return lookup;
    }

    /// <summary>
    /// Builds a dictionary mapping a key column's value to a list of rows (for 1:N relationships).
    /// </summary>
    private static Dictionary<string, List<Dictionary<string, string>>> BuildGroupLookup(
        Dictionary<string, CsvTable> csvData, string tableName, string keyColumn)
    {
        if (!csvData.TryGetValue(tableName, out var table))
            return new();

        var keyIdx = Array.IndexOf(table.Headers, keyColumn);
        if (keyIdx < 0) return new();

        var lookup = new Dictionary<string, List<Dictionary<string, string>>>();
        foreach (var row in table.Rows)
        {
            var key = keyIdx < row.Length ? row[keyIdx] : "";
            if (string.IsNullOrEmpty(key)) continue;

            var dict = new Dictionary<string, string>();
            for (var i = 0; i < table.Headers.Length && i < row.Length; i++)
                dict[table.Headers[i]] = row[i];

            if (!lookup.TryGetValue(key, out var list))
            {
                list = [];
                lookup[key] = list;
            }
            list.Add(dict);
        }
        return lookup;
    }

    /// <summary>
    /// Gets a column value from a CsvTable row by column name. Returns "" if not found.
    /// </summary>
    private static string Col(CsvTable table, string[] row, string columnName)
    {
        var idx = Array.IndexOf(table.Headers, columnName);
        return idx >= 0 && idx < row.Length ? row[idx] : "";
    }

    /// <summary>
    /// Gets a column value from a row dictionary. Returns "" if not found.
    /// </summary>
    private static string Col(Dictionary<string, string> row, string columnName)
    {
        return row.GetValueOrDefault(columnName, "");
    }

    // ── Simple 1:1 Mappings ─────────────────────────────────────────────

    /// <summary>
    /// season CSV → seasons table.
    /// </summary>
    private static void MapSeasons(Dictionary<string, CsvTable> csvData, Dictionary<string, List<string[]>> result)
    {
        if (!csvData.TryGetValue("season", out var table)) return;

        var rows = new List<string[]>(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            rows.Add([
                Col(table, row, "id"),
                Col(table, row, "year"),
                Col(table, row, "wikipedia")
            ]);
        }
        result["seasons"] = rows;
    }

    /// <summary>
    /// circuit CSV → circuits table.
    /// </summary>
    private static void MapCircuits(Dictionary<string, CsvTable> csvData, Dictionary<string, List<string[]>> result)
    {
        if (!csvData.TryGetValue("circuit", out var table)) return;

        var rows = new List<string[]>(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            rows.Add([
                Col(table, row, "id"),
                Col(table, row, "reference"),    // → circuit_ref
                Col(table, row, "name"),
                Col(table, row, "locality"),     // → location
                Col(table, row, "country"),
                Col(table, row, "latitude"),
                Col(table, row, "longitude"),
                Col(table, row, "altitude"),
                Col(table, row, "wikipedia")     // → wikipedia_url
            ]);
        }
        result["circuits"] = rows;
    }

    /// <summary>
    /// driver CSV → drivers table.
    /// </summary>
    private static void MapDrivers(Dictionary<string, CsvTable> csvData, Dictionary<string, List<string[]>> result)
    {
        if (!csvData.TryGetValue("driver", out var table)) return;

        var rows = new List<string[]>(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            rows.Add([
                Col(table, row, "id"),
                Col(table, row, "reference"),                // → driver_ref
                Col(table, row, "permanent_car_number"),     // → number
                Col(table, row, "abbreviation"),             // → code
                Col(table, row, "forename"),
                Col(table, row, "surname"),
                Col(table, row, "date_of_birth"),
                Col(table, row, "nationality"),
                Col(table, row, "wikipedia")                 // → wikipedia_url
            ]);
        }
        result["drivers"] = rows;
    }

    /// <summary>
    /// team CSV → constructors table.
    /// </summary>
    private static void MapConstructors(Dictionary<string, CsvTable> csvData, Dictionary<string, List<string[]>> result)
    {
        if (!csvData.TryGetValue("team", out var table)) return;

        var rows = new List<string[]>(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            rows.Add([
                Col(table, row, "id"),
                Col(table, row, "reference"),    // → constructor_ref
                Col(table, row, "name"),
                Col(table, row, "nationality"),
                Col(table, row, "wikipedia")     // → wikipedia_url
            ]);
        }
        result["constructors"] = rows;
    }

    // ── Races (round + session merge) ───────────────────────────────────

    /// <summary>
    /// round CSV + session CSV → races table.
    /// Merges round data with session timestamps to populate time, fp/quali/sprint dates.
    /// </summary>
    private static void MapRaces(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, List<string[]>> result,
        Dictionary<string, List<Dictionary<string, string>>> sessionsByRound)
    {
        if (!csvData.TryGetValue("round", out var table)) return;

        var rows = new List<string[]>(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            // Skip cancelled rounds
            if (Col(table, row, "is_cancelled") == "t") continue;

            var roundId = Col(table, row, "id");
            var sessions = sessionsByRound.GetValueOrDefault(roundId) ?? [];

            // Extract race time from the R session
            var raceSession = sessions.FirstOrDefault(s => Col(s, "type") == "R");
            var raceTime = ExtractTime(raceSession);

            // Extract practice, qualifying, sprint session dates/times
            var (fp1Date, fp1Time) = ExtractSessionDateTime(sessions, "FP1");
            var (fp2Date, fp2Time) = ExtractSessionDateTime(sessions, "FP2");
            var (fp3Date, fp3Time) = ExtractSessionDateTime(sessions, "FP3");
            var (qualiDate, qualiTime) = ExtractQualifyingDateTime(sessions);
            var (sprintDate, sprintTime) = ExtractSprintDateTime(sessions);

            rows.Add([
                roundId,                        // id (round.id = race.id)
                Col(table, row, "season_id"),
                Col(table, row, "number"),      // round number
                Col(table, row, "name"),
                Col(table, row, "circuit_id"),
                Col(table, row, "date"),        // race date
                raceTime,
                Col(table, row, "wikipedia"),
                fp1Date, fp1Time,
                fp2Date, fp2Time,
                fp3Date, fp3Time,
                qualiDate, qualiTime,
                sprintDate, sprintTime
            ]);
        }
        result["races"] = rows;
    }

    /// <summary>
    /// Extracts time-of-day from a session. Returns "" if has_time_data is false or time is midnight.
    /// </summary>
    private static string ExtractTime(Dictionary<string, string>? session)
    {
        if (session == null) return "";
        if (Col(session, "has_time_data") != "t") return "";

        var timestamp = Col(session, "timestamp");
        if (string.IsNullOrEmpty(timestamp)) return "";

        if (DateTimeOffset.TryParse(timestamp, out var dto) && dto.TimeOfDay != TimeSpan.Zero)
            return dto.ToString("HH:mm:ss");

        return "";
    }

    private static (string Date, string Time) ExtractSessionDateTime(
        List<Dictionary<string, string>> sessions, string type)
    {
        var session = sessions.FirstOrDefault(s => Col(s, "type") == type && Col(s, "is_cancelled") != "t");
        return ExtractDateTimePair(session);
    }

    private static (string Date, string Time) ExtractQualifyingDateTime(
        List<Dictionary<string, string>> sessions)
    {
        // Use the first qualifying session's date (Q1 or legacy types QA/QB/QO)
        var session = sessions
            .Where(s => QualifyingTypes.Contains(Col(s, "type")) && Col(s, "is_cancelled") != "t")
            .OrderBy(s => Col(s, "number"))
            .FirstOrDefault();

        return ExtractDateTimePair(session);
    }

    private static (string Date, string Time) ExtractSprintDateTime(
        List<Dictionary<string, string>> sessions)
    {
        var session = sessions
            .FirstOrDefault(s => SprintRaceTypes.Contains(Col(s, "type")) && Col(s, "is_cancelled") != "t");

        return ExtractDateTimePair(session);
    }

    private static (string Date, string Time) ExtractDateTimePair(Dictionary<string, string>? session)
    {
        if (session == null) return ("", "");

        var timestamp = Col(session, "timestamp");
        if (string.IsNullOrEmpty(timestamp)) return ("", "");

        if (!DateTimeOffset.TryParse(timestamp, out var dto)) return ("", "");

        var date = dto.ToString("yyyy-MM-dd");
        var time = Col(session, "has_time_data") == "t" && dto.TimeOfDay != TimeSpan.Zero
            ? dto.ToString("HH:mm:ss") : "";

        return (date, time);
    }

    // ── Status (derived from sessionentry.detail) ───────────────────────

    /// <summary>
    /// Builds the status table from distinct sessionentry.detail values.
    /// Returns a detail→statusId map for use by results/sprint mapping.
    /// </summary>
    private static Dictionary<string, string> BuildAndMapStatus(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, List<string[]>> result)
    {
        var statusMap = new Dictionary<string, string>();

        if (!csvData.TryGetValue("sessionentry", out var table))
        {
            result["status"] = [];
            return statusMap;
        }

        // Collect distinct detail values (sorted for deterministic IDs)
        var details = new SortedSet<string>();
        foreach (var row in table.Rows)
        {
            var detail = Col(table, row, "detail");
            if (!string.IsNullOrEmpty(detail))
                details.Add(detail);
        }

        var statusRows = new List<string[]>();
        var id = 1;
        foreach (var detail in details)
        {
            statusMap[detail] = id.ToString();
            statusRows.Add([id.ToString(), detail]);
            id++;
        }

        // Fallback status for entries with empty detail
        statusMap[""] = id.ToString();
        statusRows.Add([id.ToString(), "Unknown"]);

        result["status"] = statusRows;
        return statusMap;
    }

    // ── Session Entries → Results / Qualifying / Sprint ──────────────────

    /// <summary>
    /// Routes session entries to results, qualifying, or sprint_results based on session type.
    /// Resolves FK chain: sessionentry → roundentry → teamdriver for driver_id / constructor_id.
    /// </summary>
    private static void MapSessionEntries(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, List<string[]>> result,
        Dictionary<string, Dictionary<string, string>> sessionById,
        Dictionary<string, Dictionary<string, string>> roundEntryById,
        Dictionary<string, Dictionary<string, string>> teamDriverById,
        Dictionary<string, string> statusMap)
    {
        if (!csvData.TryGetValue("sessionentry", out var table))
        {
            result["results"] = [];
            result["qualifying"] = [];
            result["sprint_results"] = [];
            return;
        }

        var resultRows = new List<string[]>();
        var sprintRows = new List<string[]>();
        var qualifyingAgg = new Dictionary<(string raceId, string driverId), QualifyingEntry>();

        var resultId = 1;
        var sprintId = 1;

        foreach (var row in table.Rows)
        {
            var sessionId = Col(table, row, "session_id");
            if (!sessionById.TryGetValue(sessionId, out var session)) continue;

            var sessionType = Col(session, "type");

            // Skip cancelled sessions
            if (Col(session, "is_cancelled") == "t") continue;

            var roundId = Col(session, "round_id"); // round.id = race.id

            // Resolve FK chain: sessionentry → roundentry → teamdriver
            var roundEntryId = Col(table, row, "round_entry_id");
            if (!roundEntryById.TryGetValue(roundEntryId, out var roundEntry)) continue;

            var teamDriverId = Col(roundEntry, "team_driver_id");
            if (!teamDriverById.TryGetValue(teamDriverId, out var teamDriver)) continue;

            var driverId = Col(teamDriver, "driver_id");
            var constructorId = Col(teamDriver, "team_id");
            var carNumber = Col(roundEntry, "car_number");

            if (sessionType == "R")
            {
                MapRaceResult(table, row, resultRows, ref resultId,
                    roundId, driverId, constructorId, carNumber, statusMap);
            }
            else if (QualifyingTypes.Contains(sessionType))
            {
                AggregateQualifyingEntry(table, row, qualifyingAgg, sessionType,
                    roundId, driverId, constructorId, carNumber);
            }
            else if (SprintRaceTypes.Contains(sessionType))
            {
                MapSprintResult(table, row, sprintRows, ref sprintId,
                    roundId, driverId, constructorId, carNumber, statusMap);
            }
            // FP1/FP2/FP3/SQ1/SQ2/SQ3 — not mapped to output tables
        }

        result["results"] = resultRows;
        result["sprint_results"] = sprintRows;

        // Convert qualifying aggregations to rows
        var qualiRows = new List<string[]>();
        var qualiId = 1;
        foreach (var entry in qualifyingAgg.Values)
        {
            qualiRows.Add([
                qualiId.ToString(),
                entry.RaceId,
                entry.DriverId,
                entry.ConstructorId,
                string.IsNullOrEmpty(entry.Number) ? "0" : entry.Number,
                string.IsNullOrEmpty(entry.Position) ? "0" : entry.Position,
                entry.Q1,
                entry.Q2,
                entry.Q3
            ]);
            qualiId++;
        }
        result["qualifying"] = qualiRows;
    }

    private static void MapRaceResult(
        CsvTable table, string[] row, List<string[]> resultRows, ref int resultId,
        string roundId, string driverId, string constructorId, string carNumber,
        Dictionary<string, string> statusMap)
    {
        var position = Col(table, row, "position");
        var detail = Col(table, row, "detail");
        var grid = Col(table, row, "grid");
        var points = Col(table, row, "points");
        var laps = Col(table, row, "laps_completed");
        var time = Col(table, row, "time");
        var fastestLapRank = Col(table, row, "fastest_lap_rank");

        var positionText = !string.IsNullOrEmpty(position) ? position : (!string.IsNullOrEmpty(detail) ? detail : "R");
        var positionOrder = !string.IsNullOrEmpty(position) ? position : "999";
        var milliseconds = ParseMilliseconds(time);
        var statusId = statusMap.GetValueOrDefault(detail, statusMap.GetValueOrDefault("", "1"));

        resultRows.Add([
            resultId.ToString(),
            roundId,
            driverId,
            constructorId,
            carNumber,
            string.IsNullOrEmpty(grid) ? "0" : grid,
            position,            // nullable
            positionText,        // NOT NULL — position or detail text
            positionOrder,       // NOT NULL — position or 999
            string.IsNullOrEmpty(points) ? "0" : points,
            string.IsNullOrEmpty(laps) ? "0" : laps,
            time,                // nullable
            milliseconds,        // nullable
            "",                  // fastest_lap — TODO: resolve from lap data (is_entry_fastest_lap)
            fastestLapRank,      // rank
            "",                  // fastest_lap_time — TODO: resolve from lap data
            "",                  // fastest_lap_speed — TODO: resolve from lap.average_speed
            statusId
        ]);
        resultId++;
    }

    private static void AggregateQualifyingEntry(
        CsvTable table, string[] row, Dictionary<(string, string), QualifyingEntry> agg,
        string sessionType, string roundId, string driverId, string constructorId, string carNumber)
    {
        var key = (roundId, driverId);
        if (!agg.TryGetValue(key, out var entry))
        {
            entry = new QualifyingEntry
            {
                RaceId = roundId,
                DriverId = driverId,
                ConstructorId = constructorId,
                Number = carNumber
            };
            agg[key] = entry;
        }

        var time = Col(table, row, "time");
        var position = Col(table, row, "position");

        // Route to q1/q2/q3 based on session type
        switch (sessionType)
        {
            case "Q1":
                entry.Q1 = time;
                break;
            case "Q2":
                entry.Q2 = time;
                break;
            case "Q3":
                entry.Q3 = time;
                break;
            default:
                // QA/QB/QO (legacy formats) → store in q1
                if (string.IsNullOrEmpty(entry.Q1))
                    entry.Q1 = time;
                break;
        }

        // Last qualifying phase determines final position
        if (!string.IsNullOrEmpty(position))
            entry.Position = position;
    }

    private static void MapSprintResult(
        CsvTable table, string[] row, List<string[]> sprintRows, ref int sprintId,
        string roundId, string driverId, string constructorId, string carNumber,
        Dictionary<string, string> statusMap)
    {
        var position = Col(table, row, "position");
        var detail = Col(table, row, "detail");
        var grid = Col(table, row, "grid");
        var points = Col(table, row, "points");
        var laps = Col(table, row, "laps_completed");
        var time = Col(table, row, "time");

        var positionText = !string.IsNullOrEmpty(position) ? position : (!string.IsNullOrEmpty(detail) ? detail : "R");
        var positionOrder = !string.IsNullOrEmpty(position) ? position : "999";
        var milliseconds = ParseMilliseconds(time);
        var statusId = statusMap.GetValueOrDefault(detail, statusMap.GetValueOrDefault("", "1"));

        sprintRows.Add([
            sprintId.ToString(),
            roundId,
            driverId,
            constructorId,
            carNumber,
            string.IsNullOrEmpty(grid) ? "0" : grid,
            position,
            positionText,
            positionOrder,
            string.IsNullOrEmpty(points) ? "0" : points,
            string.IsNullOrEmpty(laps) ? "0" : laps,
            time,
            milliseconds,
            "",              // fastest_lap — TODO
            "",              // fastest_lap_time — TODO
            statusId
        ]);
        sprintId++;
    }

    private sealed class QualifyingEntry
    {
        public string RaceId = "";
        public string DriverId = "";
        public string ConstructorId = "";
        public string Number = "";
        public string Position = "";
        public string Q1 = "";
        public string Q2 = "";
        public string Q3 = "";
    }

    // ── Championship Standings ──────────────────────────────────────────

    /// <summary>
    /// driverchampionship CSV → driver_standings table.
    /// Filters to post-race standings only (session type = R).
    /// </summary>
    private static void MapDriverStandings(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, List<string[]>> result,
        Dictionary<string, Dictionary<string, string>> sessionById)
    {
        if (!csvData.TryGetValue("driverchampionship", out var table))
        {
            result["driver_standings"] = [];
            return;
        }

        var rows = new List<string[]>();
        var id = 1;

        foreach (var row in table.Rows)
        {
            var sessionId = Col(table, row, "session_id");

            // Only include standings computed after race sessions
            if (!string.IsNullOrEmpty(sessionId) && sessionById.TryGetValue(sessionId, out var session))
            {
                if (Col(session, "type") != "R") continue;
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                // session_id present but not found — skip
                continue;
            }

            var roundId = Col(table, row, "round_id");
            if (string.IsNullOrEmpty(roundId)) continue;

            var position = Col(table, row, "position");

            rows.Add([
                id.ToString(),
                roundId,                  // race_id
                Col(table, row, "driver_id"),
                Col(table, row, "points"),
                position,
                !string.IsNullOrEmpty(position) ? position : "",  // position_text
                Col(table, row, "win_count")
            ]);
            id++;
        }

        result["driver_standings"] = rows;
    }

    /// <summary>
    /// teamchampionship CSV → constructor_standings table.
    /// Filters to post-race standings only (session type = R).
    /// </summary>
    private static void MapConstructorStandings(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, List<string[]>> result,
        Dictionary<string, Dictionary<string, string>> sessionById)
    {
        if (!csvData.TryGetValue("teamchampionship", out var table))
        {
            result["constructor_standings"] = [];
            return;
        }

        var rows = new List<string[]>();
        var id = 1;

        foreach (var row in table.Rows)
        {
            var sessionId = Col(table, row, "session_id");

            if (!string.IsNullOrEmpty(sessionId) && sessionById.TryGetValue(sessionId, out var session))
            {
                if (Col(session, "type") != "R") continue;
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                continue;
            }

            var roundId = Col(table, row, "round_id");
            if (string.IsNullOrEmpty(roundId)) continue;

            var position = Col(table, row, "position");

            rows.Add([
                id.ToString(),
                roundId,                  // race_id
                Col(table, row, "team_id"),   // constructor_id
                Col(table, row, "points"),
                position,
                !string.IsNullOrEmpty(position) ? position : "",
                Col(table, row, "win_count")
            ]);
            id++;
        }

        result["constructor_standings"] = rows;
    }

    // ── Session Entry Context (shared by lap_times + pit_stops) ─────────

    private record SessionEntryContext(string RaceId, string DriverId, string SessionType);

    /// <summary>
    /// Builds session_entry_id → (race_id, driver_id, session_type) lookup.
    /// Resolves the full FK chain: sessionentry → session (round_id, type) + roundentry → teamdriver (driver_id).
    /// </summary>
    private static Dictionary<string, SessionEntryContext> BuildSessionEntryLookup(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, Dictionary<string, string>> sessionById,
        Dictionary<string, Dictionary<string, string>> roundEntryById,
        Dictionary<string, Dictionary<string, string>> teamDriverById)
    {
        var lookup = new Dictionary<string, SessionEntryContext>();

        if (!csvData.TryGetValue("sessionentry", out var table)) return lookup;

        foreach (var row in table.Rows)
        {
            var entryId = Col(table, row, "id");
            var sessionId = Col(table, row, "session_id");
            var roundEntryId = Col(table, row, "round_entry_id");

            if (!sessionById.TryGetValue(sessionId, out var session)) continue;
            if (!roundEntryById.TryGetValue(roundEntryId, out var roundEntry)) continue;

            var teamDriverId = Col(roundEntry, "team_driver_id");
            if (!teamDriverById.TryGetValue(teamDriverId, out var teamDriver)) continue;

            lookup[entryId] = new SessionEntryContext(
                Col(session, "round_id"),
                Col(teamDriver, "driver_id"),
                Col(session, "type"));
        }

        return lookup;
    }

    // ── Lap Times ───────────────────────────────────────────────────────

    /// <summary>
    /// lap CSV → lap_times table.
    /// Only includes laps from race sessions (type = R).
    /// </summary>
    private static void MapLapTimes(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, List<string[]>> result,
        Dictionary<string, SessionEntryContext> sessionEntryCtx)
    {
        if (!csvData.TryGetValue("lap", out var table))
        {
            result["lap_times"] = [];
            return;
        }

        var rows = new List<string[]>();
        foreach (var row in table.Rows)
        {
            var sessionEntryId = Col(table, row, "session_entry_id");
            if (!sessionEntryCtx.TryGetValue(sessionEntryId, out var ctx)) continue;
            if (ctx.SessionType != "R") continue;

            var time = Col(table, row, "time");
            var milliseconds = ParseMilliseconds(time);

            rows.Add([
                ctx.RaceId,
                ctx.DriverId,
                Col(table, row, "number"),      // lap number
                Col(table, row, "position"),
                time,
                milliseconds
            ]);
        }

        result["lap_times"] = rows;
    }

    // ── Pit Stops ───────────────────────────────────────────────────────

    /// <summary>
    /// pitstop CSV → pit_stops table.
    /// Resolves lap number through lap_id FK. Only race session pit stops.
    /// </summary>
    private static void MapPitStops(
        Dictionary<string, CsvTable> csvData,
        Dictionary<string, List<string[]>> result,
        Dictionary<string, SessionEntryContext> sessionEntryCtx,
        Dictionary<string, Dictionary<string, string>> lapById)
    {
        if (!csvData.TryGetValue("pitstop", out var table))
        {
            result["pit_stops"] = [];
            return;
        }

        var rows = new List<string[]>();
        foreach (var row in table.Rows)
        {
            var sessionEntryId = Col(table, row, "session_entry_id");
            if (!sessionEntryCtx.TryGetValue(sessionEntryId, out var ctx)) continue;
            if (ctx.SessionType != "R") continue;

            var stop = Col(table, row, "number");
            var lapId = Col(table, row, "lap_id");
            var lapNumber = "";
            if (!string.IsNullOrEmpty(lapId) && lapById.TryGetValue(lapId, out var lapRow))
            {
                lapNumber = Col(lapRow, "number");
            }

            var localTimestamp = Col(table, row, "local_timestamp");
            var duration = Col(table, row, "duration");
            var milliseconds = ParseMilliseconds(duration);

            rows.Add([
                ctx.RaceId,
                ctx.DriverId,
                stop,           // stop number
                lapNumber,      // lap
                localTimestamp,  // time (time of day)
                duration,       // duration string
                milliseconds    // duration in ms
            ]);
        }

        result["pit_stops"] = rows;
    }

    // ── Utility ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a time string (HH:MM:SS.fff or MM:SS.fff) to total milliseconds.
    /// Returns "" if parsing fails.
    /// </summary>
    private static string ParseMilliseconds(string time)
    {
        if (string.IsNullOrEmpty(time)) return "";

        if (TimeSpan.TryParse(time, out var ts))
            return ((long)ts.TotalMilliseconds).ToString();

        return "";
    }
}
