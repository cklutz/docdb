//using YamlDotNet.Serialization;
namespace DocDB;

internal static class CommandHelper
{
    public static int Run(BaseOptions options, Func<IOutput, int> run)
    {
        var output = new Output(options.Verbose, options.WarningsAsErrors);
        int rc = run(output);
        return rc == 0 ? output.HasErrors ? 1 : 0 : rc;
    }
}
