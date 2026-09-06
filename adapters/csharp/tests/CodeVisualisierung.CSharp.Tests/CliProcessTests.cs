using System.Diagnostics;
using System.Text;
using CodeVisualisierung.CSharp.Tests.Infrastructure;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class CliProcessTests
{
    [Fact]
    public void HelpIsAStableSuccessfulProcessContract()
    {
        var result = RunCli("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("codegraph-csharp <input> --output <graph.json>", result.StandardOutput);
        Assert.Contains("  5  Ausgabe- oder Dateisystemfehler", result.StandardOutput);
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public void VersionIsWrittenToStdoutWithoutDiagnostics()
    {
        var result = RunCli("--version");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0.1.0", result.StandardOutput.Trim());
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public void InvalidArgumentsUseExitCodeTwoAndStableDiagnostic()
    {
        var result = RunCli("--unknown");

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(
            "Argumentfehler: Erwartet wird codegraph-csharp <input> --output <graph.json>. Verwende --help.",
            result.StandardError.Trim());
        Assert.Empty(result.StandardOutput);
    }

    [Fact]
    public void MissingInputArgumentUsesExitCodeTwoAndStableDiagnostic()
    {
        var result = RunCli();

        Assert.Equal(2, result.ExitCode);
        Assert.Equal("Argumentfehler: Es fehlt der Eingabepfad.", result.StandardError.Trim());
        Assert.Empty(result.StandardOutput);
    }

    [Fact]
    public void MissingOutputArgumentUsesExitCodeTwoAndStableDiagnostic()
    {
        var result = RunCli("input.slnx", "--output");

        Assert.Equal(2, result.ExitCode);
        Assert.Equal("Argumentfehler: Für --output fehlt der Zielpfad.", result.StandardError.Trim());
        Assert.Empty(result.StandardOutput);
    }

    [Fact]
    public void MissingInputUsesExitCodeThreeAndDoesNotCreateOutput()
    {
        using var temp = TestTempDirectory.Create("cli-process-");
        var outputPath = temp.GetPath("graph.json");

        var result = RunCli(temp.GetPath("missing.slnx"), "--output", outputPath);

        Assert.Equal(3, result.ExitCode);
        Assert.Equal(
            "Eingabe kann nicht gelesen oder ausgewertet werden. Unterstützt werden .slnx, .sln und .csproj.",
            result.StandardError.Trim());
        Assert.Empty(result.StandardOutput);
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void ExistingSupportedInputReportsUnavailableAnalysisAtTheProcessBoundary()
    {
        using var temp = TestTempDirectory.Create("cli-process-");
        var inputPath = temp.CreateFile("sample.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var outputPath = temp.GetPath("graph.json");

        var result = RunCli(inputPath, "--output", outputPath);

        Assert.Equal(4, result.ExitCode);
        Assert.Equal("Die Analysepipeline ist noch nicht verfügbar.", result.StandardError.Trim());
        Assert.Empty(result.StandardOutput);
        Assert.False(File.Exists(outputPath));
    }

    private static CliProcessResult RunCli(params string[] arguments)
    {
        var cliAssemblyPath = FindCliAssemblyPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(cliAssemblyPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add(cliAssemblyPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Die C#-CLI konnte nicht gestartet werden.");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CliProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static string FindCliAssemblyPath()
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "codegraph-csharp.dll");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        throw new FileNotFoundException("Die gebaute C#-CLI wurde nicht gefunden.", candidate);
    }

    private sealed record CliProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
