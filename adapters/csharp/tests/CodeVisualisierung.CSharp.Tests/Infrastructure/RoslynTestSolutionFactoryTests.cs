using CodeVisualisierung.CSharp.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class RoslynTestSolutionFactoryTests(PreparedSolutionFixture solutions)
    : IClassFixture<PreparedSolutionFixture>
{
    [Fact]
    public async Task Creates_compilable_multi_project_solution_with_project_reference()
    {
        var prepared = solutions.GetOrCreate(
            "reference-mini",
            () => RoslynTestSolutionFactory.CreateSolution(
                RoslynReferenceMiniSolutionSpec.Create().ToArray()));
        var applicationProject = prepared.Solution.Projects.Single(
            project => project.Name == "CSharpReferenceMini.Application");

        var compilation = await applicationProject.GetCompilationAsync();

        Assert.NotNull(compilation);
        Assert.Contains(
            applicationProject.ProjectReferences,
            projectReference => projectReference.ProjectId == prepared.Solution.Projects.Single(
                project => project.Name == "CSharpReferenceMini.Contracts").Id);
        Assert.DoesNotContain(
            compilation!.GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(
            compilation.Assembly.GlobalNamespace.GetNamespaceMembers(),
            namespaceSymbol => namespaceSymbol.Name == "CodeVisualisierung");
    }

    [Fact]
    public void Preserves_virtual_solution_and_document_paths()
    {
        using var prepared = RoslynTestSolutionFactory.CreateSolution(
            Path.Combine("C:\\virtual", "reference-mini.slnx"),
            new RoslynProjectSpec(
                "PathProject",
                [("Source.cs", "namespace PathProject; public sealed class Source { }")],
                VirtualProjectDirectory: "src/PathProject"));
        var project = prepared.Solution.Projects.Single();
        var document = project.Documents.Single();

        Assert.Equal(
            Path.GetFullPath("C:\\virtual\\src\\PathProject\\PathProject.csproj"),
            project.FilePath);
        Assert.Equal(
            Path.GetFullPath("C:\\virtual\\src\\PathProject\\Source.cs"),
            document.FilePath);
    }

    [Fact]
    public void Rejects_unknown_project_reference()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RoslynTestSolutionFactory.CreateSolution(
                new RoslynProjectSpec(
                    "BrokenProject",
                    [("Source.cs", "namespace BrokenProject; public sealed class Source { }")],
                    ProjectReferences: ["MissingProject"])));
    }
}
