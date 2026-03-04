using System.CommandLine;
using WeRace.DataImport.Importers;

var sourceOption = new Option<DirectoryInfo>("--source") { Description = "Path to the directory containing Jolpica CSV files (formula_one_*.csv)", Required = true };
var connectionOption = new Option<string>("--connection") { Description = "PostgreSQL connection string", Required = true };
var modeOption = new Option<ImportMode>("--mode") { Description = "Import mode: full (truncate + reload) or delta (upsert)", DefaultValueFactory = _ => ImportMode.Full };

var rootCommand = new RootCommand("WeRace data import tool — loads Jolpica F1 CSV data into PostgreSQL")
{
    sourceOption,
    connectionOption,
    modeOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var source = parseResult.GetValue(sourceOption)!;
    var connection = parseResult.GetValue(connectionOption)!;
    var mode = parseResult.GetValue(modeOption);

    if (!source.Exists)
    {
        Console.Error.WriteLine($"Source directory not found: {source.FullName}");
        return;
    }

    Console.WriteLine($"WeRace Data Import");
    Console.WriteLine($"  Source: {source.FullName}");
    Console.WriteLine($"  Mode:   {mode}");
    Console.WriteLine();

    var importer = new JolpicaCsvImporter(connection);
    await importer.ImportAsync(source.FullName, mode);
});

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
