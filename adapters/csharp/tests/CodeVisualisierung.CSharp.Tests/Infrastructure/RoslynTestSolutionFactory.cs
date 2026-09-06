#nullable enable

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeVisualisierung.CSharp.Tests.Infrastructure;

/// <summary>
/// Deklarative Beschreibung eines kleinen C#-Testprojekts.
/// </summary>
public sealed record RoslynProjectSpec(
    string Name,
    IReadOnlyList<(string FileName, string Content)> Documents,
    IReadOnlyList<string>? ProjectReferences = null,
    IReadOnlyList<MetadataReference>? AdditionalReferences = null,
    NullableContextOptions Nullable = NullableContextOptions.Enable,
    IReadOnlyList<string>? PreprocessorSymbols = null,
    OutputKind OutputKind = OutputKind.DynamicallyLinkedLibrary,
    string? VirtualProjectDirectory = null);

/// <summary>
/// In-Memory-Solution und zugehöriger Workspace mit kontrollierter Lebensdauer.
/// </summary>
public sealed record RoslynTestSolution(Solution Solution, Workspace Workspace) : IDisposable
{
    public void Dispose() => Workspace.Dispose();
}

/// <summary>
/// Erstellt kleine, mehrprojektfähige Roslyn-Solutions ohne Dateien anzulegen.
/// </summary>
public static class RoslynTestSolutionFactory
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> CoreReferencesLazy =
        new(BuildCoreReferences);

    public static ImmutableArray<MetadataReference> CoreReferences => CoreReferencesLazy.Value;

    public static RoslynTestSolution CreateSolution(
        string source,
        string projectName = "TestProj",
        string documentName = "Doc.cs")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        return CreateSolution(new RoslynProjectSpec(projectName, [(documentName, source)]));
    }

    public static RoslynTestSolution CreateSolution(params RoslynProjectSpec[] projectSpecs) =>
        CreateSolutionCore(null, projectSpecs);

    public static RoslynTestSolution CreateSolution(
        string virtualSolutionFilePath,
        params RoslynProjectSpec[] projectSpecs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(virtualSolutionFilePath);

        return CreateSolutionCore(Path.GetFullPath(virtualSolutionFilePath), projectSpecs);
    }

    private static RoslynTestSolution CreateSolutionCore(
        string? virtualSolutionFilePath,
        IReadOnlyList<RoslynProjectSpec> projectSpecs)
    {
        ArgumentNullException.ThrowIfNull(projectSpecs);

        var workspace = new AdhocWorkspace();
        var solution = virtualSolutionFilePath is null
            ? workspace.CurrentSolution
            : workspace.AddSolution(SolutionInfo.Create(
                SolutionId.CreateNewId(),
                VersionStamp.Create(),
                filePath: virtualSolutionFilePath));
        var solutionDirectory = virtualSolutionFilePath is null
            ? null
            : Path.GetDirectoryName(virtualSolutionFilePath)!;
        var projectIdsByName = new Dictionary<string, ProjectId>(StringComparer.Ordinal);

        foreach (var projectSpec in projectSpecs)
        {
            if (!projectIdsByName.TryAdd(projectSpec.Name, ProjectId.CreateNewId(projectSpec.Name)))
            {
                throw new InvalidOperationException(
                    $"Projektname '{projectSpec.Name}' ist in der Test-Solution doppelt vorhanden.");
            }

            solution = AddProject(
                solution,
                projectIdsByName[projectSpec.Name],
                projectSpec,
                solutionDirectory);
        }

        foreach (var projectSpec in projectSpecs)
        {
            solution = WireProjectReferences(solution, projectSpec, projectIdsByName);
        }

        return new RoslynTestSolution(solution, workspace);
    }

    private static Solution WireProjectReferences(
        Solution solution,
        RoslynProjectSpec projectSpec,
        IReadOnlyDictionary<string, ProjectId> projectIdsByName)
    {
        if (projectSpec.ProjectReferences is null)
        {
            return solution;
        }

        var projectId = projectIdsByName[projectSpec.Name];
        foreach (var referencedName in projectSpec.ProjectReferences)
        {
            if (!projectIdsByName.TryGetValue(referencedName, out var referencedId))
            {
                throw new InvalidOperationException(
                    $"Projekt '{projectSpec.Name}' referenziert das unbekannte Projekt '{referencedName}'.");
            }

            solution = solution.AddProjectReference(projectId, new ProjectReference(referencedId));
        }

        return solution;
    }

    private static Solution AddProject(
        Solution solution,
        ProjectId projectId,
        RoslynProjectSpec projectSpec,
        string? solutionDirectory)
    {
        var references = projectSpec.AdditionalReferences is { Count: > 0 }
            ? CoreReferences.Concat(projectSpec.AdditionalReferences).ToImmutableArray()
            : CoreReferences;
        var projectDirectory = projectSpec.VirtualProjectDirectory ?? projectSpec.Name;
        var projectFilePath = solutionDirectory is null
            ? null
            : Path.GetFullPath(Path.Combine(
                solutionDirectory,
                projectDirectory,
                projectSpec.Name + ".csproj"));
        var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                projectSpec.Name,
                projectSpec.Name,
                LanguageNames.CSharp,
                filePath: projectFilePath)
            .WithMetadataReferences(references)
            .WithCompilationOptions(new CSharpCompilationOptions(
                projectSpec.OutputKind,
                nullableContextOptions: projectSpec.Nullable));

        if (projectSpec.PreprocessorSymbols is { Count: > 0 })
        {
            projectInfo = projectInfo.WithParseOptions(
                new CSharpParseOptions(preprocessorSymbols: projectSpec.PreprocessorSymbols));
        }

        solution = solution.AddProject(projectInfo);
        foreach (var (fileName, content) in projectSpec.Documents)
        {
            var documentPath = solutionDirectory is null
                ? null
                : Path.GetFullPath(Path.Combine(solutionDirectory, projectDirectory, fileName));
            solution = solution.AddDocument(
                DocumentId.CreateNewId(projectId),
                fileName,
                content,
                filePath: documentPath);
        }

        return solution;
    }

    private static ImmutableArray<MetadataReference> BuildCoreReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(System.Runtime.GCSettings).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Task).Assembly,
        };

        return assemblies
            .Select(assembly => assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(location => (MetadataReference)MetadataReference.CreateFromFile(location))
            .ToImmutableArray();
    }
}

/// <summary>
/// Hält vorbereitete In-Memory-Solutions innerhalb einer Testklasse wiederverwendbar.
/// </summary>
public sealed class PreparedSolutionFixture : IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<RoslynTestSolution>> scenarios = new();

    public RoslynTestSolution GetOrCreate(
        string scenarioName,
        Func<RoslynTestSolution> solutionFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
        ArgumentNullException.ThrowIfNull(solutionFactory);

        return scenarios.GetOrAdd(
                scenarioName,
                _ => new Lazy<RoslynTestSolution>(solutionFactory, LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }

    public void Dispose()
    {
        foreach (var scenario in scenarios.Values)
        {
            if (scenario.IsValueCreated)
            {
                scenario.Value.Dispose();
            }
        }

        scenarios.Clear();
    }
}
