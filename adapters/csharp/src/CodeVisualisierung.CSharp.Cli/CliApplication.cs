using System.Text;
using CodeVisualisierung.CSharp.Analysis;
using CodeVisualisierung.CSharp.Contract;

namespace CodeVisualisierung.CSharp.Cli;

internal static class CliApplication
{
    private const string Version = "0.1.0";
    private const string Usage = "codegraph-csharp <input> --output <graph.json>";
    private const string InputErrorMessage = "Eingabe kann nicht gelesen oder ausgewertet werden. Unterstützt werden .slnx, .sln und .csproj.";
    private const string OutputDirectoryErrorMessage = "Ausgabeverzeichnis ist nicht vorhanden oder nicht beschreibbar.";

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
        if (!TryPreparePaths(invocation, out var inputPath, out var outputPath, out var preparationError, out var preparationExitCode))
            return WriteError(standardError, preparationError, preparationExitCode);

        var analysis = await TryAnalyzeAsync(inputPath);
        if (!analysis.Succeeded)
            return WriteError(standardError, analysis.Error!, analysis.ErrorCode);

        var serialization = await TrySerializeAsync(analysis.Result!);
        if (!serialization.Succeeded)
            return WriteError(standardError, serialization.Error!, serialization.ErrorCode);

        try
        {
            WriteAtomically(outputPath, serialization.Json!);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return WriteError(standardError, "Ausgabe konnte nicht geschrieben werden: Die Zieldatei konnte nicht atomar geschrieben werden.", CliExitCodes.OutputError);
        }

        WriteDiagnostics(standardError, analysis.Result!.Diagnostics);
        WriteSummary(standardOutput, analysis.Result.Summary, Encoding.UTF8.GetByteCount(serialization.Json!));
        return analysis.Result.Summary.Status == "complete" ? CliExitCodes.Success : CliExitCodes.Partial;
    }

    private static bool TryPreparePaths(
        CliInvocation invocation,
        out string inputPath,
        out string outputPath,
        out string error,
        out int errorCode)
    {
        if (!TryGetFullPath(invocation.InputPath, out inputPath)
            || !File.Exists(inputPath)
            || !IsSupportedInput(inputPath))
        {
            outputPath = string.Empty;
            error = InputErrorMessage;
            errorCode = CliExitCodes.InputError;
            return false;
        }

        if (!TryGetFullPath(invocation.OutputPath, out outputPath)
            || !HasWritableOutputDirectory(outputPath))
        {
            error = OutputDirectoryErrorMessage;
            errorCode = CliExitCodes.OutputError;
            return false;
        }

        error = string.Empty;
        errorCode = CliExitCodes.Success;
        return true;
    }

    private static async Task<AnalysisAttempt> TryAnalyzeAsync(string inputPath)
    {
        try
        {
            return new AnalysisAttempt(await new WorkspaceAnalysis().AnalyzeAsync(inputPath), CliExitCodes.Success, null);
        }
        catch (Exception exception) when (exception is InputLoadException or InvalidDataException)
        {
            return new AnalysisAttempt(null, CliExitCodes.InputError, "Eingabe kann nicht ausgewertet werden: Die Solution- oder Projektdatei ist ungültig.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new AnalysisAttempt(null, CliExitCodes.InputError, "Eingabe kann nicht ausgewertet werden: Die Datei konnte nicht gelesen werden.");
        }
        catch (Exception)
        {
            return new AnalysisAttempt(null, CliExitCodes.AnalysisError, "Fataler Analyse- oder Vertragsfehler: Die Analyse konnte nicht abgeschlossen werden.");
        }
    }

    private static async Task<SerializationAttempt> TrySerializeAsync(AnalysisResult result)
    {
        try
        {
            var json = GraphJson.Serialize(result.Graph);
            await GraphSchemaValidator.ValidateAsync(json, Path.Combine(AppContext.BaseDirectory, "graph-universe.schema.json"));
            return new SerializationAttempt(json, CliExitCodes.Success, null);
        }
        catch (Exception)
        {
            return new SerializationAttempt(null, CliExitCodes.AnalysisError, "Fataler Analyse- oder Vertragsfehler: Die Graphausgabe konnte nicht validiert werden.");
        }
    }

    private static void WriteAtomically(string outputPath, string json)
    {
        var fullOutputPath = outputPath;
        var outputDirectory = Path.GetDirectoryName(fullOutputPath)
            ?? throw new IOException("Das Ausgabeverzeichnis konnte nicht bestimmt werden.");
        var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(fullOutputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullOutputPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch (IOException ignored)
            {
                _ = ignored;
            }
            catch (UnauthorizedAccessException ignored)
            {
                _ = ignored;
            }
        }
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

    private static bool TryGetFullPath(string path, out string fullPath)
    {
        try
        {
            fullPath = Path.GetFullPath(path);
            return true;
        }
        catch (ArgumentException)
        {
            fullPath = string.Empty;
            return false;
        }
        catch (NotSupportedException)
        {
            fullPath = string.Empty;
            return false;
        }
    }

    private static bool HasWritableOutputDirectory(string outputPath)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
            return false;

        var probePath = Path.Combine(outputDirectory, $".codegraph-write-test-{Guid.NewGuid():N}.tmp");
        try
        {
            using (File.Create(probePath))
            {
            }

            File.Delete(probePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
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

    private static void WriteDiagnostics(TextWriter output, IReadOnlyList<string> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            output.WriteLine($"Diagnose: {diagnostic}");
    }

    private sealed record AnalysisAttempt(AnalysisResult? Result, int ErrorCode, string? Error)
    {
        public bool Succeeded => Result is not null;
    }

    private sealed record SerializationAttempt(string? Json, int ErrorCode, string? Error)
    {
        public bool Succeeded => Json is not null;
    }
}
