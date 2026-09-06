namespace CodeVisualisierung.CSharp.Cli;

internal static class Program
{
    private const string Version = "0.1.0";

    public static int Main(string[] args)
    {
        if (args is ["--help"] or ["-h"])
        {
            Console.WriteLine("codegraph-csharp <input> --output <graph.json>");
            return 0;
        }

        if (args is ["--version"])
        {
            Console.WriteLine(Version);
            return 0;
        }

        Console.Error.WriteLine("Die C#-Adapter-Grundlage enthält noch keine Analysepipeline.");
        Console.Error.WriteLine("Verwende --help für die geplante CLI-Syntax.");
        return 2;
    }
}
