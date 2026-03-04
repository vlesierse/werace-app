using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace WeRace.DataImport.Importers;

/// <summary>
/// Parsed CSV table: header names and data rows.
/// </summary>
public record CsvTable(string[] Headers, List<string[]> Rows);

/// <summary>
/// Reads a directory of formula_one_*.csv files and returns parsed data keyed by table name.
/// Table names strip the "formula_one_" prefix (e.g., "season", "circuit", "driver").
/// </summary>
public static class CsvDataParser
{
    private const string FilePrefix = "formula_one_";

    /// <summary>
    /// Parses all formula_one_*.csv files in the given directory.
    /// Returns a dictionary keyed by table name (prefix stripped) to parsed headers + rows.
    /// </summary>
    public static Dictionary<string, CsvTable> Parse(string directoryPath)
    {
        var result = new Dictionary<string, CsvTable>(StringComparer.OrdinalIgnoreCase);

        var csvFiles = Directory.GetFiles(directoryPath, $"{FilePrefix}*.csv")
            .OrderBy(f => f); // Deterministic order

        foreach (var filePath in csvFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var tableName = fileName[FilePrefix.Length..];

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null,
            });

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];

            var rows = new List<string[]>();
            while (csv.Read())
            {
                var row = new string[headers.Length];
                for (var i = 0; i < headers.Length; i++)
                {
                    row[i] = csv.GetField(i) ?? "";
                }
                rows.Add(row);
            }

            result[tableName] = new CsvTable(headers, rows);
        }

        return result;
    }
}
