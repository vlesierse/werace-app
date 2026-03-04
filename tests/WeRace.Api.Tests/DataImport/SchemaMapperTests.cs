using WeRace.DataImport.Importers;

namespace WeRace.Api.Tests.DataImport;

/// <summary>
/// Tests for <see cref="SchemaMapper"/> covering table name mapping,
/// column name snake_case conversion, FK resolution, and full mapping pipeline.
/// </summary>
public class SchemaMapperTests
{
    // ── Table name mapping ─────────────────────────────────────────────

    [Theory]
    [InlineData("pitStops", "pit_stops")]
    [InlineData("lapTimes", "lap_times")]
    [InlineData("driverStandings", "driver_standings")]
    [InlineData("constructorStandings", "constructor_standings")]
    [InlineData("sprintResults", "sprint_results")]
    [InlineData("constructorResults", "constructor_results")]
    public void MapTableName_CamelCaseJolpicaTable_ReturnsSnakeCaseWeRaceName(string jolpicaTable, string expected)
    {
        var result = SchemaMapper.MapTableName(jolpicaTable);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("seasons", "seasons")]
    [InlineData("circuits", "circuits")]
    [InlineData("races", "races")]
    [InlineData("drivers", "drivers")]
    [InlineData("constructors", "constructors")]
    [InlineData("status", "status")]
    [InlineData("results", "results")]
    [InlineData("qualifying", "qualifying")]
    public void MapTableName_DirectMapTable_PreservesName(string jolpicaTable, string expected)
    {
        var result = SchemaMapper.MapTableName(jolpicaTable);

        result.Should().Be(expected);
    }

    [Fact]
    public void MapTableName_UnknownTable_ReturnsNull()
    {
        var result = SchemaMapper.MapTableName("nonexistent_table");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("pit_stops", "pit_stops")]
    [InlineData("lap_times", "lap_times")]
    [InlineData("driver_standings", "driver_standings")]
    [InlineData("sprint_results", "sprint_results")]
    [InlineData("constructor_standings", "constructor_standings")]
    [InlineData("constructor_results", "constructor_results")]
    public void MapTableName_AlreadySnakeCaseVariant_ReturnsCorrectName(string jolpicaTable, string expected)
    {
        var result = SchemaMapper.MapTableName(jolpicaTable);

        result.Should().Be(expected);
    }

    [Fact]
    public void MapTableName_CaseInsensitive_ReturnsCorrectMapping()
    {
        SchemaMapper.MapTableName("SEASONS").Should().Be("seasons");
        SchemaMapper.MapTableName("Circuits").Should().Be("circuits");
        SchemaMapper.MapTableName("PitStops").Should().Be("pit_stops");
    }

    // ── All 14 Jolpica tables mapped correctly ─────────────────────────

    [Fact]
    public void MapTableName_All14JolpicaTables_MappedCorrectly()
    {
        var expectedMappings = new Dictionary<string, string>
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
            ["pitStops"] = "pit_stops",
            ["lapTimes"] = "lap_times",
            ["driverStandings"] = "driver_standings",
            ["constructorStandings"] = "constructor_standings",
            ["constructorResults"] = "constructor_results",
        };

        foreach (var (jolpica, expected) in expectedMappings)
        {
            SchemaMapper.MapTableName(jolpica).Should().Be(expected, because: $"'{jolpica}' should map to '{expected}'");
        }
    }

    // ── snake_case conversion ──────────────────────────────────────────

    [Theory]
    [InlineData("pitStops", "pit_stops")]
    [InlineData("lapTimes", "lap_times")]
    [InlineData("driverStandings", "driver_standings")]
    [InlineData("constructorStandings", "constructor_standings")]
    [InlineData("sprintResults", "sprint_results")]
    [InlineData("raceId", "race_id")]
    [InlineData("driverId", "driver_id")]
    [InlineData("constructorId", "constructor_id")]
    [InlineData("statusId", "status_id")]
    [InlineData("seasonId", "season_id")]
    [InlineData("positionText", "position_text")]
    [InlineData("fastestLapTime", "fastest_lap_time")]
    [InlineData("fastestLapSpeed", "fastest_lap_speed")]
    public void ToSnakeCase_CamelCaseInput_ReturnsSnakeCaseOutput(string input, string expected)
    {
        var result = SchemaMapper.ToSnakeCase(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("id", "id")]
    [InlineData("name", "name")]
    [InlineData("points", "points")]
    [InlineData("round", "round")]
    [InlineData("date", "date")]
    public void ToSnakeCase_AlreadyLowerCase_PreservesValue(string input, string expected)
    {
        var result = SchemaMapper.ToSnakeCase(input);

        result.Should().Be(expected);
    }

    // ── Column order ───────────────────────────────────────────────────

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
            SchemaMapper.GetColumnOrder(table).Should().NotBeNull(because: $"column order should be defined for '{table}'");
        }
    }

    [Fact]
    public void GetColumnOrder_UnknownTable_ReturnsNull()
    {
        SchemaMapper.GetColumnOrder("nonexistent").Should().BeNull();
    }

    // ── FK resolution: races.year → races.season_id ────────────────────

    [Fact]
    public void MapAll_RacesYearColumn_ResolvedToSeasonId()
    {
        var jolpicaData = new Dictionary<string, List<string[]>>
        {
            ["seasons"] =
            [
                ["1", "2022", "http://2022"],
                ["2", "2023", "http://2023"],
            ],
            ["races"] =
            [
                // Jolpica races: [raceId, year, round, circuitId, name, date, time, url]
                ["100", "2023", "1", "77", "Bahrain Grand Prix", "2023-03-05", "15:00:00", "http://bahrain"],
                ["101", "2022", "1", "77", "Bahrain Grand Prix", "2022-03-20", "15:00:00", "http://bahrain22"],
            ]
        };

        var result = SchemaMapper.MapAll(jolpicaData);

        result.Should().ContainKey("races");
        var races = result["races"];

        // First race: year 2023 → season_id = 2
        races[0][0].Should().Be("100");        // id
        races[0][1].Should().Be("2");           // season_id (from year 2023)
        races[0][2].Should().Be("1");           // round

        // Second race: year 2022 → season_id = 1
        races[1][1].Should().Be("1");           // season_id (from year 2022)
    }

    [Fact]
    public void MapAll_RaceWithUnknownSeason_SkipsRow()
    {
        var jolpicaData = new Dictionary<string, List<string[]>>
        {
            ["seasons"] =
            [
                ["1", "2023", "http://2023"],
            ],
            ["races"] =
            [
                // Year 9999 does not exist in seasons
                ["100", "9999", "1", "77", "Mystery GP", "9999-01-01", "00:00:00", "http://mystery"],
            ]
        };

        var result = SchemaMapper.MapAll(jolpicaData);

        result.Should().ContainKey("races");
        result["races"].Should().BeEmpty();
    }

    [Fact]
    public void MapAll_RaceColumnReordering_NameAndCircuitIdSwapped()
    {
        var jolpicaData = new Dictionary<string, List<string[]>>
        {
            ["seasons"] = [["1", "2023", "http://2023"]],
            ["races"] =
            [
                // Jolpica order: [raceId, year, round, circuitId, name, date, time, url]
                ["50", "2023", "5", "10", "Monaco Grand Prix", "2023-05-28", "14:00:00", "http://monaco"],
            ]
        };

        var result = SchemaMapper.MapAll(jolpicaData);

        var race = result["races"][0];
        // WeRace order: [id, season_id, round, name, circuit_id, date, time, url]
        race[0].Should().Be("50");                  // id
        race[1].Should().Be("1");                   // season_id
        race[2].Should().Be("5");                   // round
        race[3].Should().Be("Monaco Grand Prix");   // name (was index 4 in Jolpica)
        race[4].Should().Be("10");                  // circuit_id (was index 3 in Jolpica)
        race[5].Should().Be("2023-05-28");          // date
        race[6].Should().Be("14:00:00");            // time
        race[7].Should().Be("http://monaco");       // url
    }

    // ── Pass-through tables ────────────────────────────────────────────

    [Fact]
    public void MapAll_NonRaceTable_PassesThroughUnchanged()
    {
        var jolpicaData = new Dictionary<string, List<string[]>>
        {
            ["drivers"] =
            [
                ["1", "verstappen", "1", "VER", "Max", "Verstappen", "1997-09-30", "Dutch", "http://ver"],
            ]
        };

        var result = SchemaMapper.MapAll(jolpicaData);

        result.Should().ContainKey("drivers");
        result["drivers"][0].Should().BeEquivalentTo(
            ["1", "verstappen", "1", "VER", "Max", "Verstappen", "1997-09-30", "Dutch", "http://ver"]);
    }

    // ── NormalizeValue ─────────────────────────────────────────────────

    [Theory]
    [InlineData("NULL", null)]
    [InlineData("\\N", null)]
    [InlineData("0000-00-00", null)]
    [InlineData("00:00:00", null)]
    public void NormalizeValue_SpecialNullValues_ReturnsNull(string input, string? expected)
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
}
