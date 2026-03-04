using WeRace.DataImport.Importers;

namespace WeRace.Api.Tests.DataImport;

/// <summary>
/// Tests for <see cref="CsvDataParser"/> covering CSV directory parsing,
/// header extraction, row parsing, and edge cases.
/// </summary>
public class CsvDataParserTests : IDisposable
{
    private readonly string _tempDir;

    public CsvDataParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"werace_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private void WriteCsv(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_tempDir, fileName), content);
    }

    // ── Basic parsing ──────────────────────────────────────────────────

    [Fact]
    public void Parse_SingleCsvFile_ReturnsTableWithHeadersAndRows()
    {
        WriteCsv("formula_one_season.csv",
            "id,api_id,championship_system_id,wikipedia,year\n" +
            "1,season_abc,1,https://en.wikipedia.org/wiki/1950,1950\n" +
            "2,season_def,1,https://en.wikipedia.org/wiki/1951,1951\n");

        var result = CsvDataParser.Parse(_tempDir);

        result.Should().ContainKey("season");
        result["season"].Headers.Should().BeEquivalentTo(["id", "api_id", "championship_system_id", "wikipedia", "year"]);
        result["season"].Rows.Should().HaveCount(2);
        result["season"].Rows[0][0].Should().Be("1");
        result["season"].Rows[0][4].Should().Be("1950");
    }

    [Fact]
    public void Parse_MultipleCsvFiles_ReturnsAllTables()
    {
        WriteCsv("formula_one_season.csv", "id,year\n1,1950\n");
        WriteCsv("formula_one_circuit.csv", "id,name\n1,Silverstone\n");

        var result = CsvDataParser.Parse(_tempDir);

        result.Should().ContainKey("season");
        result.Should().ContainKey("circuit");
        result.Should().HaveCount(2);
    }

    // ── Prefix stripping ───────────────────────────────────────────────

    [Fact]
    public void Parse_StripsFormulaOnePrefix_ReturnsCleanTableName()
    {
        WriteCsv("formula_one_driverchampionship.csv", "id,driver_id\n1,42\n");

        var result = CsvDataParser.Parse(_tempDir);

        result.Should().ContainKey("driverchampionship");
        result.Should().NotContainKey("formula_one_driverchampionship");
    }

    // ── Non-matching files ─────────────────────────────────────────────

    [Fact]
    public void Parse_IgnoresNonFormulaOneFiles()
    {
        WriteCsv("formula_one_season.csv", "id,year\n1,1950\n");
        WriteCsv("other_data.csv", "id,name\n1,test\n");

        var result = CsvDataParser.Parse(_tempDir);

        result.Should().HaveCount(1);
        result.Should().ContainKey("season");
    }

    // ── Empty directory ────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyDirectory_ReturnsEmptyDictionary()
    {
        var result = CsvDataParser.Parse(_tempDir);

        result.Should().BeEmpty();
    }

    // ── Quoted fields ──────────────────────────────────────────────────

    [Fact]
    public void Parse_QuotedFieldsWithCommas_ParsedCorrectly()
    {
        WriteCsv("formula_one_championshipsystem.csv",
            "id,name\n" +
            "1,\"1958, 1960, 1963-1965 Championship\"\n");

        var result = CsvDataParser.Parse(_tempDir);

        result["championshipsystem"].Rows[0][1].Should().Be("1958, 1960, 1963-1965 Championship");
    }

    // ── Empty values ───────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyValues_ReturnedAsEmptyStrings()
    {
        WriteCsv("formula_one_driver.csv", "id,abbreviation,permanent_car_number\n1,,\n");

        var result = CsvDataParser.Parse(_tempDir);

        result["driver"].Rows[0][1].Should().Be("");
        result["driver"].Rows[0][2].Should().Be("");
    }

    // ── File with only headers ─────────────────────────────────────────

    [Fact]
    public void Parse_FileWithOnlyHeaders_ReturnsEmptyRowList()
    {
        WriteCsv("formula_one_baseteam.csv", "id,api_id,name\n");

        var result = CsvDataParser.Parse(_tempDir);

        result.Should().ContainKey("baseteam");
        result["baseteam"].Headers.Should().HaveCount(3);
        result["baseteam"].Rows.Should().BeEmpty();
    }
}
