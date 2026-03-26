namespace JAMS2Dacier.Tests;

public class ConvertTests
{
    [Fact]
    public void ConvertXmlToYaml()
    {
        // Arrange
        var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var testExtractDir = Path.Combine(solutionDir, "JAMS2Dacier.Tests", "TestExtract");
        var expectedDir = Path.Combine(solutionDir, "JAMS2Dacier.Tests", "TestResults", "Expected");
        var actualDir = Path.Combine(solutionDir, "JAMS2Dacier.Tests", "TestResults", "Actual");

        if (Directory.Exists(actualDir))
            Directory.Delete(actualDir, true);
        Directory.CreateDirectory(actualDir);

        // Act: Run the converter
        var exePath = Path.Combine(solutionDir, "JAMS2Dacier", "bin", "Debug", "net10.0", "JAMS2Dacier.exe");
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Could not find converter exe at {exePath}");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"convert \"{testExtractDir}\" \"{actualDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        string stdOut = proc.StandardOutput.ReadToEnd();
        string stdErr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            throw new Exception($"JAMS2Dacier exited with code {proc.ExitCode}\nSTDOUT:\n{stdOut}\nSTDERR:\n{stdErr}");
        }

        // Assert: Compare actual and expected files
        var expectedFiles = Directory.GetFiles(expectedDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(expectedDir, f)).ToList();
        var actualFiles = Directory.GetFiles(actualDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(actualDir, f)).ToList();

        var allFiles = new HashSet<string>(expectedFiles.Concat(actualFiles));
        var diffs = new List<string>();
        foreach (var relPath in allFiles)
        {
            var expectedFile = Path.Combine(expectedDir, relPath);
            var actualFile = Path.Combine(actualDir, relPath);
            if (!File.Exists(expectedFile))
            {
                diffs.Add($"Extra file in actual: {relPath}");
                continue;
            }
            if (!File.Exists(actualFile))
            {
                diffs.Add($"Missing file in actual: {relPath}");
                continue;
            }
            var expectedText = File.ReadAllText(expectedFile).Replace("\r\n", "\n");
            var actualText = File.ReadAllText(actualFile).Replace("\r\n", "\n");
            if (expectedText != actualText)
            {
                diffs.Add($"Difference in file {relPath}:\nEXPECTED:\n{expectedText}\nACTUAL:\n{actualText}");
            }
        }
        if (diffs.Count > 0)
        {
            var msg = string.Join("\n\n", diffs);
            Assert.Fail($"Discrepancies found:\n{msg}");
        }
    }
}
