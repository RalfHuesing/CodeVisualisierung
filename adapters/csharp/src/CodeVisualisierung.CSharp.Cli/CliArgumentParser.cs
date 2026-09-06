namespace CodeVisualisierung.CSharp.Cli;

internal static class CliArgumentParser
{
    public static CliParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return CliParseResult.WithError("Argumentfehler: Es fehlt der Eingabepfad.");
        }

        if (args is ["--help"])
        {
            return CliParseResult.ForCommand(CliCommand.Help);
        }

        if (args is ["--version"])
        {
            return CliParseResult.ForCommand(CliCommand.Version);
        }

        if (args is [_, "--output"])
        {
            return CliParseResult.WithError("Argumentfehler: Für --output fehlt der Zielpfad.");
        }

        if (args.Length != 3 || args[1] != "--output")
        {
            return CliParseResult.WithError(
                "Argumentfehler: Erwartet wird codegraph-csharp <input> --output <graph.json>. Verwende --help.");
        }

        if (string.IsNullOrWhiteSpace(args[0]))
        {
            return CliParseResult.WithError("Argumentfehler: Es fehlt der Eingabepfad.");
        }

        if (string.IsNullOrWhiteSpace(args[2]))
        {
            return CliParseResult.WithError("Argumentfehler: Für --output fehlt der Zielpfad.");
        }

        return CliParseResult.ForInvocation(new CliInvocation(args[0], args[2]));
    }
}

internal enum CliCommand
{
    None,
    Help,
    Version
}

internal sealed record CliInvocation(string InputPath, string OutputPath);

internal sealed record CliParseResult(
    CliCommand Command,
    CliInvocation? Invocation,
    string? Error)
{
    public static CliParseResult ForCommand(CliCommand command) => new(command, null, null);

    public static CliParseResult ForInvocation(CliInvocation invocation) =>
        new(CliCommand.None, invocation, null);

    public static CliParseResult WithError(string error) => new(CliCommand.None, null, error);
}
