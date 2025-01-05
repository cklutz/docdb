using Spectre.Console.Cli;

namespace DocDB;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("docdb");
            config.UseAssemblyInformationalVersion();
            config.UseStrictParsing();
            config.SetExceptionHandler((e, _) => Output.WriteException(e));

            config.AddCommand<MetadataCommand>("metadata")
                .WithExample("metadata", "connection-string.txt")
                .WithExample("metadata", "\"%MY_CONN_STR%\"")
                .WithExample("metadata", "\"Data Source=.;Initial Catalog=MyDatabase;Integrated Security=True;Encrypt=False\"");

        });

        return app.Run(args);
    }
}
