using System.Diagnostics;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class AiNetLinterTests
{
    [Fact]
    public void AiNetLinterAcceptsTheAdapterSolution()
    {
        var taskRoot = FindTaskRoot();
        var executablePath = ResolveLinterPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = taskRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--path");
        startInfo.ArgumentList.Add(Path.Combine(taskRoot, "CodeVisualisierung.CSharp.slnx"));
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add(Path.Combine(taskRoot, "rules.json"));
        startInfo.ArgumentList.Add("--no-cache");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("AiNetLinter konnte nicht gestartet werden.");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"AiNetLinter meldete Exit-Code {process.ExitCode}.{Environment.NewLine}" +
            $"stdout:{Environment.NewLine}{standardOutput}{Environment.NewLine}" +
            $"stderr:{Environment.NewLine}{standardError}");
    }

    private static string ResolveLinterPath()
    {
        var defaultPath = Path.Combine("C:\\", "Daten", "Tools", "AiNetLinter-win-x64", "AiNetLinter.exe");
        var configuredPath = Environment.GetEnvironmentVariable("AINETLINTER_EXE");
        var executablePath = string.IsNullOrWhiteSpace(configuredPath) ? defaultPath : configuredPath;

        Assert.True(File.Exists(executablePath), $"AiNetLinter wurde nicht gefunden: {executablePath}");
        return executablePath;
    }

    private static string FindTaskRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ainetlinter.project.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Der C#-Adapter-Taskroot wurde nicht gefunden.");
    }
}
