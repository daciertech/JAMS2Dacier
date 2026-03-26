using System.CommandLine;
using System.Diagnostics;

namespace JAMS2Dacier;

internal class ExtractCommand : Command
{
    public ExtractCommand() : base("extract", "Extracts JAMS Definitions as XML files")
    {
        var serverNameArgument = new Argument<string>("server")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The name of your JAMS Server."
        };
        Arguments.Add(serverNameArgument);

        var outputDirectoryArgument = new Argument<string>("output")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The path to the output XML files."
        };
        Arguments.Add(outputDirectoryArgument);

        var bypassOption = new Option<bool>("--bypass")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Bypass the PowerShell execution policy"
        };
        Options.Add(bypassOption);

        this.SetAction(parseResult => ExtractCommandAction(
            parseResult,
            parseResult.GetValue(serverNameArgument),
            parseResult.GetValue(outputDirectoryArgument),
            parseResult.GetValue(bypassOption)));
    }

    public async Task ExtractCommandAction(
        ParseResult parseResult,
        string? serverName,
        string? outputDirectory,
        bool bypassOption)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("The 'extract' command is only supported on Windows.");
            return;
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            Console.WriteLine("Server name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            Console.WriteLine("Output directory is required.");
            return;
        }

        //
        //  The script is installed in "tools" when it is installed as a .NET Tool
        //  otherwise it is in the same directory as the executable when running from source
        //
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "tools", "Extract-Definitions.ps1");
        if (!File.Exists(scriptPath))
        {
            scriptPath = Path.Combine(AppContext.BaseDirectory, "Extract-Definitions.ps1");
        }
        var fullOutputDirectory = Path.GetFullPath(outputDirectory);

        //
        //  I don't really understand why we have to add an extra backslash but, we do.
        //  If we don't, the output directory in Arguments ends with a quote instead of a backslash and quote.
        //
        if (fullOutputDirectory.EndsWith("\\"))
            fullOutputDirectory += "\\";

        var bypassValue = bypassOption ? "-ExecutionPolicy Bypass" : string.Empty;

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive {bypassValue} -File \"{scriptPath}\" {serverName} \"{fullOutputDirectory}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        //Console.WriteLine($"Starting PowerShell process with command: {psi.FileName} {psi.Arguments}");

        using var process = Process.Start(psi);
        if (process is null)
        {
            Console.WriteLine("Failed to start PowerShell process.");
            return;
        }
        Console.WriteLine($"PowerShell process started, PID: {process.Id}, waiting for it to complete...");

        await process.WaitForExitAsync();

        Console.WriteLine($"PowerShell process exited with code {process.ExitCode}.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        Console.WriteLine(output);

        if (process.ExitCode != 0)
        {
            Console.WriteLine("Script failed:");
            Console.WriteLine(error);
        }
    }
}
