using WeRace.DataImport.Importers;

namespace WeRace.Api.Tests.DataImport;

/// <summary>
/// Tests for <see cref="SchemaMapper"/> covering CSV-to-WeRace table mapping,
/// FK chain resolution, status derivation, and value normalization.
/// </summary>
public class SchemaMapperTests
{
    // ── Column order definitions ───────────────────────────────────────

    [Fact]
    public void GetColumnOrder_AllWeRaceTables_HaveColumnDefinitions()
    {
        var tables = new[]
        {
            "seasons", "circuits", "races", "drivers", "constructors", "status",
            "results", "qualifying", "sprint_results",
            "pit_stops", "lap_times",
            "driver_standings", "constructor_standings", "constructor_results"
        };

        foreach (var table in tables)
        {
            SchemaMapper.GetColumnOrder(table).Should().NotBeNull(
                because: $"column order should be defined for '{table}'");
        }
    }

    [Fact]
    public void GetColumnOrder_UnknownTable_ReturnsNull()
    {
        SchemaMapper.GetColumnOrder("nonexistent").Should().BeNull();
    }

    // ── NormalizeValue ─────────────────────────────────────────────────

    [Theory]
    [InlineData("", null)]
    [InlineData("NULL", null)]
    [InlineData("\\N", null)]
    public void NormalizeValue_NullMarkers_ReturnsNull(string input, string? expected)
    {
        SchemaMapper.NormalizeValue(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("123", "123")]
    [InlineData("2023-03-05", "2023-03-05")]
    public void NormalizeValue_RegularValue_ReturnsUnchanged(string input, string expected)
    {
        SchemaMapper.NormalizeValue(input).Should().Be(expected);
    }

    // ── Seasons mapping ────────────────────────────────────────────────

    [Fact]
    public void MapAll_Seasons_MapsIdYearWikipedia()
    {
        var csvData = new Dictionary<string, CsvTable>
        {
            ["season"] = new(
                ["id", "api_id", "championship_system_id", "wikipedia", "year"],
                [
                    ["1", "season_abc", "1", "https://en.wikipedia.org/wiki/1950", "1950"],
                    ["2", "season_def", "1", "https://en.wikipedia.org/wiki/1951", "1951"],
                ])
        };

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("seasons");
        result["seasons"].Should().HaveCount(2);
        result["seasons"][0].Should().BeEquivalentTo(["1", "1950", "https://en.wikipedia.org/wiki/1950"]);
        result["seasons"][1].Should().BeEquivalentTo(["2", "1951", "https://en.wikipedia.org/wiki/1951"]);
    }

    // ── Circuits mapping ───────────────────────────────────────────────

    [Fact]
    public void MapAll_Circuits_MapsColumnsCorrectly()
    {
        var csvData = new Dictionary<string, CsvTable>
        {
            ["circuit"] = new(
                ["id", "altitude", "api_id", "country", "country_code", "latitude", "locality", "longitude", "name", "reference", "wikipedia"],
                [
                    ["1", "153", "circuit_abc", "UK", "GBR", "52.0786", "Silverstone", "-1.01694", "Silverstone Circuit", "silverstone", "https://en.wikipedia.org/wiki/Silverstone_Circuit"],
                ])
        };

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("circuits");
        // Expected order: id, circuit_ref, name, location, country, lat, lon, alt, wikipedia_url
        var circuit = result["circuits"][0];
        circuit[0].Should().Be("1");                           // id
        circuit[1].Should().Be("silverstone");                 // circuit_ref (from reference)
        circuit[2].Should().Be("Silverstone Circuit");         // name
        circuit[3].Should().Be("Silverstone");                 // location (from locality)
        circuit[4].Should().Be("UK");                          // country
        circuit[5].Should().Be("52.0786");                     // latitude
        circuit[6].Should().Be("-1.01694");                    // longitude
        circuit[7].Should().Be("153");                         // altitude
    }

    // ── Drivers mapping ────────────────────────────────────────────────

    [Fact]
    public void MapAll_Drivers_MapsColumnsCorrectly()
    {
        var csvData = new Dictionary<string, CsvTable>
        {
            ["driver"] = new(
                ["id", "abbreviation", "api_id", "country_code", "date_of_birth", "forename", "nationality", "permanent_car_number", "reference", "surname", "wikipedia"],
                [
                    ["1", "VER", "driver_abc", "NLD", "1997-09-30", "Max", "Dutch", "33", "verstappen", "Verstappen", "https://en.wikipedia.org/wiki/Max_Verstappen"],
                ])
        };

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("drivers");
        var driver = result["drivers"][0];
        // Expected order: id, driver_ref, number, code, forename, surname, dob, nationality, wikipedia_url
        driver[0].Should().Be("1");
        driver[1].Should().Be("verstappen");     // driver_ref (from reference)
        driver[2].Should().Be("33");             // number (from permanent_car_number)
        driver[3].Should().Be("VER");            // code (from abbreviation)
        driver[4].Should().Be("Max");
        driver[5].Should().Be("Verstappen");
        driver[6].Should().Be("1997-09-30");
        driver[7].Should().Be("Dutch");
    }

    // ── Constructors mapping ───────────────────────────────────────────

    [Fact]
    public void MapAll_Constructors_MapsColumnsCorrectly()
    {
        var csvData = new Dictionary<string, CsvTable>
        {
            ["team"] = new(
                ["id", "api_id", "base_team_id", "country_code", "name", "nationality", "reference", "wikipedia"],
                [
                    ["1", "team_abc", "", "GBR", "McLaren", "British", "mclaren", "https://en.wikipedia.org/wiki/McLaren"],
                ])
        };

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("constructors");
        var constructor = result["constructors"][0];
        // Expected: id, constructor_ref, name, nationality, wikipedia_url
        constructor[0].Should().Be("1");
        constructor[1].Should().Be("mclaren");   // constructor_ref (from reference)
        constructor[2].Should().Be("McLaren");
        constructor[3].Should().Be("British");
    }

    // ── Races mapping with session data ────────────────────────────────

    [Fact]
    public void MapAll_Races_MergesRoundAndSessionData()
    {
        var csvData = new Dictionary<string, CsvTable>
        {
            ["round"] = new(
                ["id", "api_id", "circuit_id", "date", "is_cancelled", "name", "number", "race_number", "season_id", "wikipedia"],
                [
                    ["1", "round_abc", "5", "2023-03-05", "f", "Bahrain Grand Prix", "1", "1", "74", "https://en.wikipedia.org/wiki/2023_Bahrain_Grand_Prix"],
                ]),
            ["session"] = new(
                ["id", "api_id", "has_time_data", "is_cancelled", "number", "point_system_id", "round_id", "scheduled_laps", "timestamp", "timezone", "type"],
                [
                    ["1", "session_r", "t", "f", "5", "10", "1", "57", "2023-03-05 15:00:00+00:00", "Asia/Bahrain", "R"],
                    ["2", "session_q1", "t", "f", "2", "1", "1", "", "2023-03-04 14:00:00+00:00", "Asia/Bahrain", "Q1"],
                    ["3", "session_fp1", "t", "f", "1", "1", "1", "", "2023-03-03 12:00:00+00:00", "Asia/Bahrain", "FP1"],
                ])
        };

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("races");
        var race = result["races"][0];
        // Expected: id, season_id, round, name, circuit_id, date, time, wikipedia_url, fp1-sprint
        race[0].Should().Be("1");                       // id (round.id)
        race[1].Should().Be("74");                      // season_id
        race[2].Should().Be("1");                       // round number
        race[3].Should().Be("Bahrain Grand Prix");      // name
        race[4].Should().Be("5");                       // circuit_id
        race[5].Should().Be("2023-03-05");              // date
        race[6].Should().Be("15:00:00");                // time (from R session)
    }

    [Fact]
    public void MapAll_Races_SkipsCancelledRounds()
    {
        var csvData = new Dictionary<string, CsvTable>
        {
            ["round"] = new(
                ["id", "api_id", "circuit_id", "date", "is_cancelled", "name", "number", "race_number", "season_id", "wikipedia"],
                [
                    ["1", "round_abc", "5", "2023-03-05", "t", "Cancelled GP", "1", "1", "74", ""],
                ])
        };

        var result = SchemaMapper.MapAll(csvData);

        result["races"].Should().BeEmpty();
    }

    // ── Status derived from session entries ─────────────────────────────

    [Fact]
    public void MapAll_Status_CollectsDistinctDetailsFromSessionEntries()
    {
        var csvData = BuildMinimalDataForResults();

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("status");
        var statusTexts = result["status"].Select(r => r[1]).ToList();
        statusTexts.Should().Contain("Finished");
        statusTexts.Should().Contain("+1 Lap");
        // Should also contain Unknown fallback
        statusTexts.Should().Contain("Unknown");
    }

    // ── Results mapping with FK chain resolution ────────────────────────

    [Fact]
    public void MapAll_Results_ResolvesDriverAndConstructorThroughFKChain()
    {
        var csvData = BuildMinimalDataForResults();

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("results");
        result["results"].Should().HaveCount(2);

        // First result entry
        var r1 = result["results"][0];
        // Columns: id, race_id, driver_id, constructor_id, number, grid, position, ...
        r1[1].Should().Be("1");     // race_id = round_id (from session.round_id)
        r1[2].Should().Be("42");    // driver_id (from teamdriver)
        r1[3].Should().Be("99");    // constructor_id / team_id (from teamdriver)
        r1[4].Should().Be("33");    // car_number (from roundentry)
    }

    // ── Qualifying aggregation ──────────────────────────────────────────

    [Fact]
    public void MapAll_Qualifying_AggregatesQ1Q2Q3PerDriverPerRace()
    {
        var csvData = BuildMinimalDataForQualifying();

        var result = SchemaMapper.MapAll(csvData);

        result.Should().ContainKey("qualifying");
        result["qualifying"].Should().HaveCount(1);

        var q = result["qualifying"][0];
        // Columns: id, race_id, driver_id, constructor_id, number, position, q1, q2, q3
        q[1].Should().Be("1");          // race_id
        q[2].Should().Be("42");         // driver_id
        q[6].Should().Be("01:31.500");  // q1
        q[7].Should().Be("01:30.200");  // q2
        q[8].Should().Be("01:29.800");  // q3
    }

    // ── Helpers for building test data ──────────────────────────────────

    /// <summary>
    /// Builds minimal CSV dataset with full FK chain for testing results mapping:
    /// session → round, sessionentry → roundentry → teamdriver
    /// </summary>
    private static Dictionary<string, CsvTable> BuildMinimalDataForResults()
    {
        return new Dictionary<string, CsvTable>
        {
            ["session"] = new(
                ["id", "api_id", "has_time_data", "is_cancelled", "number", "point_system_id", "round_id", "scheduled_laps", "timestamp", "timezone", "type"],
                [
                    ["1", "s1", "t", "f", "3", "10", "1", "57", "2023-03-05 15:00:00+00:00", "Asia/Bahrain", "R"],
                ]),
            ["round"] = new(
                ["id", "api_id", "circuit_id", "date", "is_cancelled", "name", "number", "race_number", "season_id", "wikipedia"],
                [
                    ["1", "r1", "5", "2023-03-05", "f", "Bahrain GP", "1", "1", "74", ""],
                ]),
            ["teamdriver"] = new(
                ["id", "api_id", "driver_id", "role", "season_id", "team_id"],
                [
                    ["10", "td1", "42", "", "74", "99"],
                    ["11", "td2", "43", "", "74", "98"],
                ]),
            ["roundentry"] = new(
                ["id", "api_id", "car_number", "round_id", "team_driver_id"],
                [
                    ["100", "re1", "33", "1", "10"],
                    ["101", "re2", "11", "1", "11"],
                ]),
            ["sessionentry"] = new(
                ["id", "api_id", "detail", "fastest_lap_rank", "grid", "is_classified", "is_eligible_for_points", "laps_completed", "points", "position", "round_entry_id", "session_id", "status", "time"],
                [
                    ["1", "se1", "Finished", "1", "1", "t", "t", "57", "25", "1", "100", "1", "0", "01:33:56.736"],
                    ["2", "se2", "+1 Lap", "", "2", "t", "t", "56", "18", "2", "101", "1", "1", ""],
                ]),
        };
    }

    /// <summary>
    /// Builds minimal CSV dataset for testing qualifying aggregation.
    /// </summary>
    private static Dictionary<string, CsvTable> BuildMinimalDataForQualifying()
    {
        return new Dictionary<string, CsvTable>
        {
            ["session"] = new(
                ["id", "api_id", "has_time_data", "is_cancelled", "number", "point_system_id", "round_id", "scheduled_laps", "timestamp", "timezone", "type"],
                [
                    ["1", "sq1", "t", "f", "1", "1", "1", "", "2023-03-04 14:00:00+00:00", "Asia/Bahrain", "Q1"],
                    ["2", "sq2", "t", "f", "2", "1", "1", "", "2023-03-04 14:30:00+00:00", "Asia/Bahrain", "Q2"],
                    ["3", "sq3", "t", "f", "3", "1", "1", "", "2023-03-04 15:00:00+00:00", "Asia/Bahrain", "Q3"],
                ]),
            ["round"] = new(
                ["id", "api_id", "circuit_id", "date", "is_cancelled", "name", "number", "race_number", "season_id", "wikipedia"],
                [
                    ["1", "r1", "5", "2023-03-05", "f", "Bahrain GP", "1", "1", "74", ""],
                ]),
            ["teamdriver"] = new(
                ["id", "api_id", "driver_id", "role", "season_id", "team_id"],
                [
                    ["10", "td1", "42", "", "74", "99"],
                ]),
            ["roundentry"] = new(
                ["id", "api_id", "car_number", "round_id", "team_driver_id"],
                [
                    ["100", "re1", "33", "1", "10"],
                ]),
            ["sessionentry"] = new(
                ["id", "api_id", "detail", "fastest_lap_rank", "grid", "is_classified", "is_eligible_for_points", "laps_completed", "points", "position", "round_entry_id", "session_id", "status", "time"],
                [
                    ["1", "se1", "", "", "1", "t", "t", "", "0", "3", "100", "1", "0", "01:31.500"],
                    ["2", "se2", "", "", "1", "t", "t", "", "0", "2", "100", "2", "0", "01:30.200"],
                    ["3", "se3", "", "", "1", "t", "t", "", "0", "1", "100", "3", "0", "01:29.800"],
                ]),
        };
    }
}
