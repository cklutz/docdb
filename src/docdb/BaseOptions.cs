using Spectre.Console.Cli;
using System.ComponentModel;


//using YamlDotNet.Serialization;

namespace DocDB;

internal class BaseOptions : CommandSettings
{
    [Description("Set log level to verbose")]
    [CommandOption("--verbose")]
    public bool Verbose { get; set; }

    [Description("Treats warnings as errors")]
    [CommandOption("--warnings-as-errors")]
    public bool WarningsAsErrors { get; set; }
}
