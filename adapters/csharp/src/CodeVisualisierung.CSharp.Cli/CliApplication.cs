namespace CodeVisualisierung.CSharp.Cli;

internal static class CliApplication
{
    private const string Version = "0.1.0";
    private const string Usage = "codegraph-csharp <input> --output <graph.json>";

    public static int Run(string[] args, TextWriter standardOutput, TextWriter standardError)
    {
        var parseResult = CliArgumentParser.Parse(args);
        if (parseResult.Error is not null)
        {
            standardError.WriteLine(parseResult.Error);
            return CliExitCodes.ArgumentError;
        }

        if (parseResult.Command == CliCommand.Help)
        {
            WriteHelp(standardOutput);
            return CliExitCodes.Success;
        }

        if (parseResult.Command == CliCommand.Version)
        {
            standardOutput.WriteLine(Version);
            return CliExitCodes.Success;
        }

        var invocation = parseResult.Invocation!;
        if (!File.Exists(invocation.InputPath) || !IsSupportedInput(invocation.InputPath))
        {
            standardError.WriteLine(
                "Eingabe kann nicht gelesen oder ausgewertet werden. Unterstützt werden .slnx, .sln und .csproj.");
            return CliExitCodes.InputError;
        }

        if (!HasExistingOutputDirectory(invocation.OutputPath))
        {
            standardError.WriteLine("Ausgabeverzeichnis ist nicht vorhanden oder nicht beschreibbar.");
            return CliExitCodes.OutputError;
        }

        standardError.WriteLine("Die Analysepipeline ist noch nicht verfügbar.");
        return CliExitCodes.AnalysisError;
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
}
