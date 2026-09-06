namespace CodeVisualisierung.CSharp.Fixtures;

internal static class CSharpReferenceMiniFixture
{
    private static readonly Lazy<string> RootPathValue = new(FindRootPath);

    public static string RootPath => RootPathValue.Value;

    public static string SolutionPath => Path.Combine(RootPath, "CSharpReferenceMini.slnx");

    private static string FindRootPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "adapters",
                "csharp",
                "tests",
                "Fixtures",
                "CSharpReferenceMini");
            if (File.Exists(Path.Combine(candidate, "CSharpReferenceMini.slnx")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Die physische CSharpReferenceMini-Fixture wurde nicht gefunden.");
    }
}
