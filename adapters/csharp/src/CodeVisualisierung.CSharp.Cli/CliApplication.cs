using System.Text;
using CodeVisualisierung.CSharp.Analysis;
using CodeVisualisierung.CSharp.Contract;

namespace CodeVisualisierung.CSharp.Cli;

internal static class CliApplication
{
    private const string Version = "0.1.0";
    private const string Usage = "codegraph-csharp <input> --output <graph.json>";

    public static async Task<int> RunAsync(string[] args, TextWriter standardOutput, TextWriter standardError)
    {
        var parseResult = CliArgumentParser.Parse(args);
        if (parseResult.Error is not null)
            return WriteError(standardError, parseResult.Error, CliExitCodes.ArgumentError);
        if (parseResult.Command == CliCommand.Help)
            return WriteHelpAndSucceed(standardOutput);
        if (parseResult.Command == CliCommand.Version)
            return WriteVersionAndSucceed(standardOutput);
        return await RunAnalysisAsync(parseResult.Invocation!, standardOutput, standardError);
    }

    private static async Task<int> RunAnalysisAsync(CliInvocation invocation, TextWriter standardOutput, TextWriter standardError)
    {
        if (!File.Exists(invocation.InputPath) || !IsSupportedInput(invocation.InputPath))
            return WriteError(standardError, "Eingabe kann nicht gelesen oder ausgewertet werden. Unterstützt werden .slnx, .sln und .csproj.", CliExitCodes.InputError);
        if (!HasExistingOutputDirectory(invocation.OutputPath))
            return WriteError(standardError, "Ausgabeverzeichnis ist nicht vorhanden oder nicht beschreibbar.", CliExitCodes.OutputError);
        try
        {
            var result = await new WorkspaceAnalysis().AnalyzeAsync(invocation.InputPath);
            var json = GraphJson.Serialize(result.Graph);
            await GraphSchemaValidator.ValidateAsync(json, Path.Combine(AppContext.BaseDirectory, "graph-universe.schema.json"));
            WriteAtomically(invocation.OutputPath, json);
            WriteSummary(standardOutput, result.Summary, Encoding.UTF8.GetByteCount(json));
            return result.Summary.Status == "complete" ? CliExitCodes.Success : CliExitCodes.Partial;
        }
        catch (InputLoadException exception)
        {
            return WriteError(standardError, $"Eingabe kann nicht ausgewertet werden: {exception.Message}", CliExitCodes.InputError);
        }
        catch (InvalidDataException exception)
        {
            return WriteError(standardError, $"Eingabe kann nicht ausgewertet werden: {exception.Message}", CliExitCodes.InputError);
        }
        catch (IOException exception)
        {
            return WriteError(standardError, $"Ausgabe konnte nicht geschrieben werden: {exception.Message}", CliExitCodes.OutputError);
        }
        catch (Exception exception) when (exception is InvalidOperationException or UnauthorizedAccessException)
        {
            return WriteError(standardError, $"Fataler Analyse- oder Vertragsfehler: {exception.Message}", CliExitCodes.AnalysisError);
        }
    }

    private static void WriteAtomically(string outputPath, string json)
    {
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath)!;
        var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(fullOutputPath)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temporaryPath, fullOutputPath, overwrite: true);
    }

    private static int WriteError(TextWriter output, string message, int exitCode)
    {
        output.WriteLine(message);
        return exitCode;
    }

    private static int WriteHelpAndSucceed(TextWriter output)
    {
        WriteHelp(output);
        return CliExitCodes.Success;
    }

    private static int WriteVersionAndSucceed(TextWriter output)
    {
        output.WriteLine(Version);
        return CliExitCodes.Success;
    }

    private static bool HasExistingOutputDirectory(string outputPath)
    {
        try
        {
            var fullOutputPath = Path.GetFullPath(outputPath);
            var outputDirectory = Path.GetDirectoryName(fullOutputPath);
            return !string.IsNullOrEmpty(outputDirectory) && Directory.Exists(outputDirectory);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static bool IsSupportedInput(string inputPath)
    {
        var extension = Path.GetExtension(inputPath);
        return extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine(Usage);
        output.WriteLine();
        output.WriteLine("Analysiert eine C#-Solution oder ein C#-Projekt und schreibt Graph-JSON.");
        output.WriteLine();
        output.WriteLine("Optionen:");
        output.WriteLine("  --help       Diese Hilfe anzeigen.");
        output.WriteLine("  --version    Version anzeigen.");
        output.WriteLine("  --output     Zielpfad für graph.json (erforderlich bei Analyse).");
        output.WriteLine();
        output.WriteLine("Exit-Codes:");
        output.WriteLine("  0  vollständiger Erfolg oder Hilfe/Version");
        output.WriteLine("  1  valider, partieller Lauf mit Ausgabe");
        output.WriteLine("  2  Argumentfehler");
        output.WriteLine("  3  Eingabe nicht lesbar oder nicht auswertbar");
        output.WriteLine("  4  fataler Analyse- oder Vertragsfehler");
        output.WriteLine("  5  Ausgabe- oder Dateisystemfehler");
    }

    private static void WriteSummary(TextWriter output, AnalysisSummary summary, int outputBytes)
    {
        output.WriteLine($"status={summary.Status}");
        output.WriteLine($"input.kind={summary.InputKind}");
        output.WriteLine($"projects.loaded={summary.ProjectsLoaded} projects.analyzed={summary.ProjectsAnalyzed} projects.skipped={summary.ProjectsSkipped} projects.failed={summary.ProjectsFailed}");
        output.WriteLine($"documents.analyzed={summary.DocumentsAnalyzed} documents.skipped={summary.DocumentsSkipped} documents.failed={summary.DocumentsFailed}");
        output.WriteLine($"nodes.emitted={summary.NodesEmitted} links.emitted={summary.LinksEmitted}");
        output.WriteLine($"relations.externalDropped={summary.ExternalDropped} relations.unresolved={summary.Unresolved}");
        output.WriteLine($"diagnostics.warnings={summary.Warnings} diagnostics.errors={summary.Errors}");
        output.WriteLine($"output.bytes={outputBytes}");
    }
}
