using System.ComponentModel;
using Spectre.Console.Cli;

namespace DocDB;

[Description("Generate YAML files from database")]
internal class MetadataCommandOptions : BaseOptions
{
    private const string DefaultOutputFolder = ".\\metadata";

    [Description($"The output base directory (default: '{DefaultOutputFolder}')")]
    [CommandOption("-o|--output")]
    public string? OutputFolder { get; set; } = DefaultOutputFolder;

    [Description("An alternative database (display) name")]
    [CommandOption("--display-database-name")]
    public string? DisplayDatabaseName { get; set; }

    [Description("A SQL (SELECT) statement to get the schema version of the database")]
    [CommandOption("--schema-version-query")]
    public string? SchemaVersionQuery { get; set; }

    [Description("A literal connection string or a the name of an environment variable or file that contains the connection string")]
    [CommandArgument(0, "[connstr]")]
    public string ConnectionString { get; set; } = null!;
}
