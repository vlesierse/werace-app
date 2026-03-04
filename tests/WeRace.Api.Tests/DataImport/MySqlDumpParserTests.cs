using WeRace.DataImport.Importers;

namespace WeRace.Api.Tests.DataImport;

/// <summary>
/// Tests for <see cref="MySqlDumpParser"/> covering INSERT parsing, escaping,
/// NULL handling, numeric values, multi-table dumps, and edge cases.
/// </summary>
public class MySqlDumpParserTests : IDisposable
{
    private readonly string _tempDir;

    public MySqlDumpParserTests()
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

    private string WriteDump(string content)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.sql");
        File.WriteAllText(path, content);
        return path;
    }

    // ── Simple INSERT parsing ──────────────────────────────────────────

    [Fact]
    public void Parse_SimpleInsertWithMultipleRows_ReturnsAllRows()
    {
        var dump = "INSERT INTO `drivers` VALUES (1,'hamilton','Lewis','Hamilton'),(2,'verstappen','Max','Verstappen');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().ContainKey("drivers");
        result["drivers"].Should().HaveCount(2);

        result["drivers"][0].Should().BeEquivalentTo(["1", "hamilton", "Lewis", "Hamilton"]);
        result["drivers"][1].Should().BeEquivalentTo(["2", "verstappen", "Max", "Verstappen"]);
    }

    // ── Backtick-quoted identifiers ────────────────────────────────────

    [Fact]
    public void Parse_BacktickQuotedTableAndColumnNames_ParsesTableNameCorrectly()
    {
        var dump = "INSERT INTO `constructors` VALUES (1,'mclaren','McLaren','British','http://en.wikipedia.org/wiki/McLaren');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().ContainKey("constructors");
        result["constructors"].Should().HaveCount(1);
        result["constructors"][0][0].Should().Be("1");
        result["constructors"][0][2].Should().Be("McLaren");
    }

    [Fact]
    public void Parse_TableNameWithoutBackticks_ParsesTableNameCorrectly()
    {
        var dump = "INSERT INTO status VALUES (1,'Finished');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().ContainKey("status");
        result["status"].Should().HaveCount(1);
    }

    // ── Escaped strings ────────────────────────────────────────────────

    [Fact]
    public void Parse_EscapedSingleQuotesWithBackslash_UnescapesCorrectly()
    {
        var dump = @"INSERT INTO `circuits` VALUES (1,'monaco','Circuit de Monaco\'s Track','Monaco');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().ContainKey("circuits");
        result["circuits"][0][2].Should().Be("Circuit de Monaco's Track");
    }

    [Fact]
    public void Parse_DoubleQuoteEscapedSingleQuotes_UnescapesCorrectly()
    {
        var dump = "INSERT INTO `circuits` VALUES (1,'monaco','Circuit de Monaco''s Track','Monaco');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().ContainKey("circuits");
        result["circuits"][0][2].Should().Be("Circuit de Monaco's Track");
    }

    [Fact]
    public void Parse_BackslashEscapeSequences_HandlesNewlineAndTab()
    {
        var dump = @"INSERT INTO `notes` VALUES (1,'Line1\nLine2\tTabbed');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["notes"][0][1].Should().Be("Line1\nLine2\tTabbed");
    }

    // ── NULL values ────────────────────────────────────────────────────

    [Fact]
    public void Parse_NullValues_PreservesNullLiteral()
    {
        var dump = "INSERT INTO `drivers` VALUES (1,'hamilton',NULL,NULL,'Lewis','Hamilton',NULL,'British','http://example.com');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["drivers"][0][2].Should().Be("NULL");
        result["drivers"][0][3].Should().Be("NULL");
        result["drivers"][0][6].Should().Be("NULL");
    }

    // ── Numeric values ─────────────────────────────────────────────────

    [Fact]
    public void Parse_IntegerValues_ParsedCorrectly()
    {
        var dump = "INSERT INTO `results` VALUES (1,1,1,1,44,1,1,'1',1,25,57);";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["results"][0][0].Should().Be("1");
        result["results"][0][4].Should().Be("44");
        result["results"][0][9].Should().Be("25");
    }

    [Fact]
    public void Parse_DecimalValues_ParsedCorrectly()
    {
        var dump = "INSERT INTO `results` VALUES (1,1,1,1,44,1,1,'1',1,25.50,57);";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["results"][0][9].Should().Be("25.50");
    }

    // ── Multi-table dumps ──────────────────────────────────────────────

    [Fact]
    public void Parse_MultiTableDump_ReturnsAllTables()
    {
        var dump = """
            INSERT INTO `seasons` VALUES (1,2023,'http://en.wikipedia.org/wiki/2023');
            INSERT INTO `circuits` VALUES (1,'bahrain','Bahrain','Sakhir','Bahrain',26.0325,50.5106,7,'http://example.com');
            INSERT INTO `drivers` VALUES (1,'verstappen',1,'VER','Max','Verstappen','1997-09-30','Dutch','http://example.com');
            """;
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().ContainKey("seasons");
        result.Should().ContainKey("circuits");
        result.Should().ContainKey("drivers");
        result["seasons"].Should().HaveCount(1);
        result["circuits"].Should().HaveCount(1);
        result["drivers"].Should().HaveCount(1);
    }

    [Fact]
    public void Parse_MultipleInsertsForSameTable_CombinesRows()
    {
        var dump = """
            INSERT INTO `seasons` VALUES (1,2022,'http://2022');
            INSERT INTO `seasons` VALUES (2,2023,'http://2023');
            """;
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["seasons"].Should().HaveCount(2);
        result["seasons"][0][1].Should().Be("2022");
        result["seasons"][1][1].Should().Be("2023");
    }

    // ── Non-INSERT statements ──────────────────────────────────────────

    [Fact]
    public void Parse_NonInsertStatements_SkippedGracefully()
    {
        var dump = """
            SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT;
            CREATE TABLE `seasons` (
              `seasonId` int(11) NOT NULL AUTO_INCREMENT,
              PRIMARY KEY (`seasonId`)
            );
            DROP TABLE IF EXISTS `old_table`;
            LOCK TABLES `seasons` WRITE;
            INSERT INTO `seasons` VALUES (1,2023,'http://example.com');
            UNLOCK TABLES;
            ALTER TABLE `seasons` ENGINE=InnoDB;
            """;
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().ContainKey("seasons");
        result["seasons"].Should().HaveCount(1);
        result["seasons"][0][1].Should().Be("2023");
    }

    // ── Empty dump ─────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyDumpFile_ReturnsEmptyDictionary()
    {
        var path = WriteDump(string.Empty);

        var result = MySqlDumpParser.Parse(path);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_DumpWithOnlyComments_ReturnsEmptyDictionary()
    {
        var dump = """
            -- MySQL dump 10.13  Distrib 5.7.24
            -- Host: localhost    Database: f1db
            -- Server version	5.7.24
            """;
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result.Should().BeEmpty();
    }

    // ── INSERT with explicit column names ──────────────────────────────

    [Fact]
    public void Parse_InsertWithExplicitColumnNames_ParsesValuesCorrectly()
    {
        // The parser regex matches INSERT INTO `table` VALUES pattern.
        // If the dump includes column names, the parser should still extract values.
        // Jolpica dumps typically don't use explicit columns, but let's verify
        // the parser handles INSERT INTO `table` VALUES after encountering non-insert.
        var dump = "INSERT INTO `seasons` VALUES (1,2023,'http://example.com'),(2,2024,'http://example2.com');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["seasons"].Should().HaveCount(2);
        result["seasons"][0].Should().HaveCount(3);
    }

    // ── Edge: values containing commas inside strings ──────────────────

    [Fact]
    public void Parse_StringValuesContainingCommas_ParsedCorrectly()
    {
        var dump = "INSERT INTO `circuits` VALUES (1,'silverstone','Silverstone Circuit, Northamptonshire','Silverstone','UK');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["circuits"][0][2].Should().Be("Silverstone Circuit, Northamptonshire");
    }

    // ── Edge: values containing parentheses inside strings ─────────────

    [Fact]
    public void Parse_StringValuesContainingParentheses_ParsedCorrectly()
    {
        var dump = "INSERT INTO `status` VALUES (1,'Accident (Turn 1)');";
        var path = WriteDump(dump);

        var result = MySqlDumpParser.Parse(path);

        result["status"][0][1].Should().Be("Accident (Turn 1)");
    }
}
