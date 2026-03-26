using System.CommandLine;

namespace JAMS2Dacier;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("JAMS2Dacier conversion tool");

        rootCommand.Subcommands.Add(new ConvertCommand());
        rootCommand.Subcommands.Add(new ExtractCommand());

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }
}
