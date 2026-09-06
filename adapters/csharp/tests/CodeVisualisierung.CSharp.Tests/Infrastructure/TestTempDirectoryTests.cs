using Xunit;

namespace CodeVisualisierung.CSharp.Tests.Infrastructure;

public sealed class TestTempDirectoryTests
{
    [Fact]
    public void CreateUsesRepositoryTempAndDisposeDeletesDirectory()
    {
        string path;
        var root = Path.GetFullPath(TestTempDirectory.RootTempDirectory) + Path.DirectorySeparatorChar;

        using (var temp = TestTempDirectory.Create("lifecycle-"))
        {
            path = temp.DirectoryPath;
            var filePath = temp.CreateFile("nested/data.txt", "content");

            Assert.StartsWith(root, path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("content", File.ReadAllText(filePath));
        }

        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void GetPathRejectsPathsOutsideManagedDirectory()
    {
        using var temp = TestTempDirectory.Create("path-guard-");

        Assert.Throws<ArgumentException>(() => temp.GetPath("..\\outside.txt"));
    }
}
