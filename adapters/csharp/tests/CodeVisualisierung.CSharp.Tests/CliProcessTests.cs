using System.Diagnostics;
using System.Text;
using CodeVisualisierung.CSharp.Contract;
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
    public void ExistingSupportedInputProducesAValidGraphAtTheProcessBoundary()
    {
        using var temp = TestTempDirectory.Create("cli-process-");
        var inputPath = temp.CreateFile("sample.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        var outputPath = temp.GetPath("graph.json");

        var result = RunCliWithCulture("en-US", inputPath, "--output", outputPath);

        Assert.Contains(result.ExitCode, new[] { 0, 1 });
        Assert.Matches("status=(complete|partial)", result.StandardOutput);
        Assert.Contains("input.kind=csproj", result.StandardOutput);
        Assert.True(string.IsNullOrEmpty(result.StandardError), result.StandardError);
        Assert.True(File.Exists(outputPath));
        Assert.Contains("\"graph-universe\"", File.ReadAllText(outputPath));
    }

    [Fact]
    public void ReferenceSolutionExportsOwnSourceInventarWithoutGeneratedFiles()
    {
        using var temp = TestTempDirectory.Create("cli-inventory-");
        var fixturePath = Path.Combine(
            FindRepositoryRoot(),
            "adapters",
            "csharp",
            "tests",
            "Fixtures",
            "CSharpReferenceMini",
            "CSharpReferenceMini.slnx");
        var firstOutput = temp.GetPath("first.json");
        var secondOutput = temp.GetPath("second.json");

        var first = RunCli(fixturePath, "--output", firstOutput);
        var second = RunCli(fixturePath, "--output", secondOutput);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(first.StandardOutput, second.StandardOutput);
        Assert.Equal(File.ReadAllText(firstOutput), File.ReadAllText(secondOutput));
        Assert.DoesNotContain("obj/", File.ReadAllText(firstOutput));
        Assert.DoesNotContain(".g.cs", File.ReadAllText(firstOutput));
        Assert.DoesNotContain("LinkedSource.cs", File.ReadAllText(firstOutput));
        Assert.Contains("project-reference", File.ReadAllText(firstOutput));
    }

    [Fact]
    public void MissingReferencedProjectIsAnInputErrorAndPreservesExistingOutput()
    {
        using var temp = TestTempDirectory.Create("cli-input-error-");
        var inputPath = temp.CreateFile("broken.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"missing.csproj\" /></ItemGroup></Project>");
        var outputPath = temp.CreateFile("graph.json", "existing");

        var result = RunCli(inputPath, "--output", outputPath);

        Assert.Equal(3, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Equal("existing", File.ReadAllText(outputPath));
    }

    [Fact]
    public async Task ReferenceSolutionOutputUsesTheSharedSchema()
    {
        using var temp = TestTempDirectory.Create("cli-schema-");
        var fixturePath = Path.Combine(FindRepositoryRoot(), "adapters", "csharp", "tests", "Fixtures", "CSharpReferenceMini", "CSharpReferenceMini.slnx");
        var outputPath = temp.GetPath("graph.json");

        Assert.Equal(0, RunCli(fixturePath, "--output", outputPath).ExitCode);
        await GraphSchemaValidator.ValidateAsync(
            File.ReadAllText(outputPath),
            Path.Combine(Path.GetDirectoryName(FindCliAssemblyPath())!, "graph-universe.schema.json"));
    }

    [Fact]
    public void LegacySolutionInputIsSupported()
    {
        using var temp = TestTempDirectory.Create("cli-sln-");
        var fixturePath = Path.Combine(FindRepositoryRoot(), "adapters", "csharp", "tests", "Fixtures", "CSharpReferenceMini", "CSharpReferenceMini.sln");
        var outputPath = temp.GetPath("graph.json");

        var result = RunCli(fixturePath, "--output", outputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("input.kind=sln", result.StandardOutput);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void MissingOutputDirectoryUsesExitCodeFiveWithoutChangingInputProcessing()
    {
        using var temp = TestTempDirectory.Create("cli-output-error-");
        var inputPath = Path.Combine(FindRepositoryRoot(), "adapters", "csharp", "tests", "Fixtures", "CSharpReferenceMini", "CSharpReferenceMini.slnx");
        var outputPath = temp.GetPath("missing-directory\\graph.json");

        var result = RunCli(inputPath, "--output", outputPath);

        Assert.Equal(5, result.ExitCode);
        Assert.Equal("Ausgabeverzeichnis ist nicht vorhanden oder nicht beschreibbar.", result.StandardError.Trim());
        Assert.Empty(result.StandardOutput);
    }

    [Fact]
    public void UnparseableSolutionUsesExitCodeThreeAndPreservesExistingOutput()
    {
        using var temp = TestTempDirectory.Create("cli-unparseable-");
        var inputPath = temp.CreateFile("broken.slnx", "<Solution>");
        var outputPath = temp.CreateFile("graph.json", "unchanged");

        var result = RunCli(inputPath, "--output", outputPath);

        Assert.Equal(3, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Contains("Eingabe kann nicht ausgewertet werden", result.StandardError);
        Assert.Equal("unchanged", File.ReadAllText(outputPath));
    }

    [Fact]
    public void CompilerDiagnosticsProduceAValidPartialOutput()
    {
        using var temp = TestTempDirectory.Create("cli-partial-");
        var inputPath = temp.CreateFile("partial.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        temp.CreateFile("Broken.cs", "public class Broken { public void Method( { }");
        var outputPath = temp.GetPath("graph.json");

        var result = RunCliWithCulture("en-US", inputPath, "--output", outputPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("status=partial", result.StandardOutput);
        Assert.Contains("diagnostics.errors=", result.StandardOutput);
        Assert.Contains("Diagnose: Compilerfehler", result.StandardError);
        Assert.DoesNotContain("Identifier expected", result.StandardError, StringComparison.Ordinal);
        Assert.True(File.Exists(outputPath));
        Assert.Contains("\"graph-universe\"", File.ReadAllText(outputPath));
    }

    [Fact]
    public void MissingPublishedSchemaUsesExitCodeFourAndPreservesExistingOutput()
    {
        using var temp = TestTempDirectory.Create("cli-contract-error-");
        var cliAssemblyPath = PublishCli(temp);
        File.Delete(Path.Combine(Path.GetDirectoryName(cliAssemblyPath)!, "graph-universe.schema.json"));
        var fixturePath = Path.Combine(FindRepositoryRoot(), "adapters", "csharp", "tests", "Fixtures", "CSharpReferenceMini", "CSharpReferenceMini.slnx");
        var outputPath = temp.CreateFile("graph.json", "unchanged");

        var result = RunCliAt(cliAssemblyPath, temp.DirectoryPath, fixturePath, "--output", outputPath);

        Assert.Equal(4, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Contains("Fataler Analyse- oder Vertragsfehler", result.StandardError);
        Assert.Equal("unchanged", File.ReadAllText(outputPath));
    }

    [Fact]
    public void PublishedCliSupportsAllInputKindsFromAnArbitraryWorkingDirectory()
    {
        using var temp = TestTempDirectory.Create("cli-published-");
        var cliAssemblyPath = PublishCli(temp);
        var fixtureRoot = Path.Combine(FindRepositoryRoot(), "adapters", "csharp", "tests", "Fixtures", "CSharpReferenceMini");
        var inputs = new[]
        {
            (Path.Combine(fixtureRoot, "CSharpReferenceMini.slnx"), "slnx"),
            (Path.Combine(fixtureRoot, "CSharpReferenceMini.sln"), "sln"),
            (Path.Combine(fixtureRoot, "src", "CSharpReferenceMini.Application", "CSharpReferenceMini.Application.csproj"), "csproj")
        };

        foreach (var (inputPath, inputKind) in inputs)
        {
            var outputPath = temp.GetPath($"{inputKind}.json");
            var result = RunCliAt(cliAssemblyPath, temp.DirectoryPath, inputPath, "--output", outputPath);

            Assert.True(result.ExitCode == 0, $"Published CLI fehlgeschlagen: stdout={result.StandardOutput} stderr={result.StandardError}");
            Assert.Equal($"input.kind={inputKind}", result.StandardOutput.Split(Environment.NewLine)[1]);
            Assert.Empty(result.StandardError);
            Assert.True(File.Exists(outputPath));
            Assert.DoesNotContain(FindRepositoryRoot(), File.ReadAllText(outputPath), StringComparison.OrdinalIgnoreCase);
        }
    }

    private static CliProcessResult RunCli(params string[] arguments)
    {
        var cliAssemblyPath = FindCliAssemblyPath();
        return RunCliAt(cliAssemblyPath, Path.GetDirectoryName(cliAssemblyPath)!, arguments);
    }

    private static CliProcessResult RunCliWithCulture(string culture, params string[] arguments)
    {
        var cliAssemblyPath = FindCliAssemblyPath();
        return RunCliAt(cliAssemblyPath, Path.GetDirectoryName(cliAssemblyPath)!, culture, arguments);
    }

    private static CliProcessResult RunCliAt(string cliAssemblyPath, string workingDirectory, params string[] arguments)
        => RunCliAt(cliAssemblyPath, workingDirectory, null, arguments);

    private static CliProcessResult RunCliAt(
        string cliAssemblyPath,
        string workingDirectory,
        string? culture,
        string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        if (culture is not null)
        {
            startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = culture;
            startInfo.Environment["LANG"] = culture;
            startInfo.Environment["LC_ALL"] = culture;
        }
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

    private static string PublishCli(TestTempDirectory temp)
    {
        var publishDirectory = temp.CreateSubdirectory("published-cli");
        var projectPath = Path.Combine(FindRepositoryRoot(), "adapters", "csharp", "src", "CodeVisualisierung.CSharp.Cli", "CodeVisualisierung.CSharp.Cli.csproj");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = FindRepositoryRoot(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { "publish", projectPath, "--no-restore", "--nologo", "-c", "Release", "-o", publishDirectory, "--verbosity", "quiet" })
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Die veröffentlichte C#-CLI konnte nicht gebaut werden.");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"dotnet publish fehlgeschlagen: {standardOutput}{standardError}");
        return Path.Combine(publishDirectory, "codegraph-csharp.dll");
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "adapters", "csharp", "CodeVisualisierung.CSharp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Das Repository wurde nicht gefunden.");
    }

    private sealed record CliProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
