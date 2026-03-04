using System.Text;
using System.Text.RegularExpressions;

namespace WeRace.DataImport.Importers;

/// <summary>
/// Parses a MySQL dump file, extracting INSERT statements per table.
/// Handles MySQL-specific syntax: backticks, string escaping, AUTO_INCREMENT.
/// </summary>
public static partial class MySqlDumpParser
{
    // Matches: INSERT INTO `tablename` VALUES (...),...;
    [GeneratedRegex(@"^INSERT\s+INTO\s+`?(\w+)`?\s+VALUES\s*", RegexOptions.IgnoreCase)]
    private static partial Regex InsertRegex();

    /// <summary>
    /// Parses a MySQL dump file and returns a dictionary of table name to list of row values.
    /// Each row is a string array of raw SQL value literals (unquoted strings, numeric literals, NULL).
    /// </summary>
    public static Dictionary<string, List<string[]>> Parse(string filePath)
    {
        var result = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(filePath, Encoding.UTF8);
        string? line;
        var sb = new StringBuilder();
        var inInsert = false;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.TrimStart();

            // Skip comments, SET, CREATE TABLE, LOCK/UNLOCK, etc.
            if (trimmed.StartsWith("--") || trimmed.StartsWith("/*") ||
                trimmed.StartsWith("SET ") || trimmed.StartsWith("CREATE ") ||
                trimmed.StartsWith("DROP ") || trimmed.StartsWith("LOCK ") ||
                trimmed.StartsWith("UNLOCK ") || trimmed.StartsWith("ALTER ") ||
                string.IsNullOrWhiteSpace(trimmed))
            {
                // If we were in a multi-line insert, finalize it
                if (inInsert && sb.Length > 0)
                {
                    ProcessInsertStatement(sb.ToString(), result);
                    sb.Clear();
                    inInsert = false;
                }
                continue;
            }

            if (InsertRegex().IsMatch(trimmed))
            {
                // Finalize any previous insert
                if (inInsert && sb.Length > 0)
                {
                    ProcessInsertStatement(sb.ToString(), result);
                    sb.Clear();
                }
                inInsert = true;
            }

            if (inInsert)
            {
                sb.Append(trimmed);

                // Check if the statement ends (ends with ;)
                if (trimmed.EndsWith(';'))
                {
                    ProcessInsertStatement(sb.ToString(), result);
                    sb.Clear();
                    inInsert = false;
                }
            }
        }

        // Finalize any remaining insert
        if (inInsert && sb.Length > 0)
        {
            ProcessInsertStatement(sb.ToString(), result);
        }

        return result;
    }

    private static void ProcessInsertStatement(string statement, Dictionary<string, List<string[]>> result)
    {
        var match = InsertRegex().Match(statement);
        if (!match.Success) return;

        var tableName = match.Groups[1].Value;
        var searchFrom = match.Groups[1].Index + match.Groups[1].Length;
        var valuesStart = statement.IndexOf("VALUES", searchFrom, StringComparison.OrdinalIgnoreCase);
        if (valuesStart < 0) return;

        // Move past "VALUES"
        var pos = valuesStart + 6;

        if (!result.TryGetValue(tableName, out var rows))
        {
            rows = [];
            result[tableName] = rows;
        }

        // Parse each (val1, val2, ...) tuple
        while (pos < statement.Length)
        {
            // Find opening paren
            pos = statement.IndexOf('(', pos);
            if (pos < 0) break;
            pos++; // skip '('

            var values = ParseValueTuple(statement, ref pos);
            if (values.Length > 0)
            {
                rows.Add(values);
            }
        }
    }

    /// <summary>
    /// Parses a single value tuple from position after '(' and advances pos past ')'.
    /// </summary>
    private static string[] ParseValueTuple(string statement, ref int pos)
    {
        var values = new List<string>();
        var sb = new StringBuilder();

        while (pos < statement.Length)
        {
            var ch = statement[pos];

            switch (ch)
            {
                case ')':
                    // End of tuple
                    values.Add(sb.ToString());
                    sb.Clear();
                    pos++; // skip ')'
                    return values.ToArray();

                case ',':
                    values.Add(sb.ToString());
                    sb.Clear();
                    pos++;
                    break;

                case '\'':
                    // Quoted string — read until unescaped closing quote
                    pos++; // skip opening quote
                    sb.Clear();
                    while (pos < statement.Length)
                    {
                        ch = statement[pos];
                        if (ch == '\\' && pos + 1 < statement.Length)
                        {
                            // MySQL escape sequences
                            var next = statement[pos + 1];
                            sb.Append(next switch
                            {
                                '\'' => '\'',
                                '\\' => '\\',
                                'n' => '\n',
                                'r' => '\r',
                                't' => '\t',
                                '0' => '\0',
                                _ => next
                            });
                            pos += 2;
                        }
                        else if (ch == '\'' && pos + 1 < statement.Length && statement[pos + 1] == '\'')
                        {
                            // Double-quote escape
                            sb.Append('\'');
                            pos += 2;
                        }
                        else if (ch == '\'')
                        {
                            // End of string
                            pos++; // skip closing quote
                            break;
                        }
                        else
                        {
                            sb.Append(ch);
                            pos++;
                        }
                    }
                    // Don't clear sb — it contains the parsed string value
                    break;

                case ' ':
                case '\t':
                case '\n':
                case '\r':
                    // Skip whitespace between values
                    pos++;
                    break;

                default:
                    // Numeric literal or NULL — read until comma or closing paren
                    sb.Clear();
                    while (pos < statement.Length)
                    {
                        ch = statement[pos];
                        if (ch == ',' || ch == ')')
                            break;
                        sb.Append(ch);
                        pos++;
                    }
                    break;
            }
        }

        return values.ToArray();
    }
}
