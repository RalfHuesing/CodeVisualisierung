using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class CliFoundationTests
{
    [Fact]
    public void SolutionContainsCliAndTestProjects()
    {
        var solutionPath = FindSolutionPath();
        var solutionText = File.ReadAllText(solutionPath);

        Assert.Contains("src/CodeVisualisierung.CSharp.Cli/CodeVisualisierung.CSharp.Cli.csproj", solutionText);
        Assert.Contains("tests/CodeVisualisierung.CSharp.Tests/CodeVisualisierung.CSharp.Tests.csproj", solutionText);
    }

    private static string FindSolutionPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "CodeVisualisierung.CSharp.slnx");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Die C#-Solution wurde nicht gefunden.");
    }
}
