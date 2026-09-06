using System.Text.Json;
using CodeVisualisierung.CSharp.Analysis;
using CodeVisualisierung.CSharp.Contract;
using CodeVisualisierung.CSharp.Fixtures;
using CodeVisualisierung.CSharp.Tests.Infrastructure;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class DeclarationPipelineTests
{
    [Fact]
    public async Task SymbolIdentityPreservesOverloadsGenericsAndTypeParameterOwnership()
    {
        using var solution = RoslynTestSolutionFactory.CreateSolution(
            "namespace Demo; public class Sample { public void Convert(int value) { } public void Convert(string value) { } public T Echo<T>(T value) => value; }");
        var project = solution.Solution.Projects.Single();
        var compilation = (await project.GetCompilationAsync())!;
        var type = compilation.GetTypeByMetadataName("Demo.Sample")!;
        var overloads = type.GetMembers("Convert").OfType<IMethodSymbol>().ToArray();
        var generic = Assert.Single(type.GetMembers("Echo").OfType<IMethodSymbol>());

        Assert.NotEqual(
            CSharpSymbolIdentity.CreateId("method", "project:Demo", overloads[0]),
            CSharpSymbolIdentity.CreateId("method", "project:Demo", overloads[1]));
        Assert.Contains("Echo<T>(T)", CSharpSymbolIdentity.GetCanonicalSignature(generic));
        var typeParameter = Assert.Single(generic.TypeParameters);
        var typeParameterId = CSharpSymbolIdentity.CreateId("type-parameter", "project:Demo", typeParameter);
        Assert.Contains("Echo<T>(T)::T#0", typeParameterId);
    }

    [Fact]
    public async Task ReferenceMiniEmitsCompleteDeclarationNodesWithStableDetails()
    {
        _ = typeof(WorkspaceAnalysis);
        var result = await AnalyzeReferenceMiniAsync();
        var declarations = result.Graph.Nodes.Where(node => IsDeclarationType(node.TypeId)).ToArray();

        AssertRequiredDeclarationTypes(declarations);
        AssertPartialTypeDetails(declarations);
        AssertDeclarationDetails(result.Graph, declarations);
        AssertLocalFunctionIdentityAndParents(result.Graph, declarations);
        AssertGeneratedCodeIsExcluded(result.Graph, declarations);
        Assert.All(declarations, AssertHasRelativeSourcePosition);
    }

    [Fact]
    public async Task ReferenceMiniOutputPassesTheSharedSchemaAndContainsNoExternalTargets()
    {
        var result = await AnalyzeReferenceMiniAsync();
        var json = GraphJson.Serialize(result.Graph);
        Assert.DoesNotContain(FindRepositoryRoot(), json, StringComparison.OrdinalIgnoreCase);
        var schemaPath = Path.Combine(
            FindRepositoryRoot(),
            "contracts",
            "graph-universe",
            "schema",
            "graph-universe.schema.json");

        await GraphSchemaValidator.ValidateAsync(json, schemaPath);
        var nodeIds = result.Graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        Assert.All(result.Graph.Links, link =>
        {
            Assert.Contains(link.Source, nodeIds);
            Assert.Contains(link.Target, nodeIds);
        });
        Assert.DoesNotContain(result.Graph.Nodes, node => node.Id.Contains("LinkedSource", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Graph.Nodes, node => node.Id.Contains("obj/", StringComparison.Ordinal));
    }

    private static void AssertRequiredDeclarationTypes(IReadOnlyCollection<GraphNode> declarations)
    {
        AssertContainerDeclarationTypes(declarations);
        AssertMemberDeclarationTypes(declarations);
    }

    private static void AssertContainerDeclarationTypes(IReadOnlyCollection<GraphNode> declarations)
    {
        Assert.Contains(declarations, node => node.TypeId == "class" && node.Label == "PartialCoordinator");
        Assert.Contains(declarations, node => node.TypeId == "interface" && node.Label == "IProcessor");
        Assert.Contains(declarations, node => node.TypeId == "record" && node.Label == "ValueToken");
        Assert.Contains(declarations, node => node.TypeId == "struct" && node.Label == "MutableCounter");
        Assert.Contains(declarations, node => node.TypeId == "enum" && node.Label == "ProcessingState");
        Assert.Contains(declarations, node => node.TypeId == "delegate" && node.Label == "StringTransformer");
        Assert.Contains(declarations, node => node.TypeId == "constructor" && node.Label == "GreetingService");
    }

    private static void AssertMemberDeclarationTypes(IReadOnlyCollection<GraphNode> declarations)
    {
        Assert.Contains(declarations, node => node.TypeId == "constructor");
        Assert.Contains(declarations, node => node.TypeId == "property" && node.Label == "Value");
        Assert.Contains(declarations, node => node.TypeId == "field" && node.Label == "increments");
        Assert.Contains(declarations, node => node.TypeId == "event" && node.Label == "Executed");
        Assert.Contains(declarations, node => node.TypeId == "operator");
        Assert.Contains(declarations, node => node.TypeId == "local-function" && node.Label == "Format");
        Assert.Contains(declarations, node => node.TypeId == "type-parameter" && node.Label == "T");
    }

    private static void AssertPartialTypeDetails(IReadOnlyCollection<GraphNode> declarations)
    {
        var partial = Assert.Single(declarations, node => node.TypeId == "class" && node.Label == "PartialCoordinator");
        Assert.Equal(2, partial.Attributes["partialDeclarationCount"]);
        Assert.Equal(
            2,
            ((JsonElement)JsonSerializer.SerializeToElement(partial.Attributes["sourcePositions"])).GetArrayLength());
    }

    private static void AssertDeclarationDetails(GraphDocument graph, IReadOnlyCollection<GraphNode> declarations)
    {
        var nodeIds = graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var node in declarations)
        {
            Assert.NotEmpty(Assert.IsType<string>(node.Attributes["qualifiedName"]));
            Assert.NotEmpty(Assert.IsType<string>(node.Attributes["signature"]));
            Assert.NotEmpty(Assert.IsType<string>(node.Attributes["accessibility"]));
            var source = Assert.IsType<SourcePosition>(node.Attributes["source"]);
            var positions = Assert.IsType<SourcePosition[]>(node.Attributes["sourcePositions"]);
            Assert.NotEmpty(positions);
            Assert.Equal(source, positions[0]);
            Assert.All(positions, AssertHasRelativeSourcePosition);

            if (node.Attributes.TryGetValue("containerId", out var containerId))
                Assert.Contains(Assert.IsType<string>(containerId), nodeIds);
        }
    }

    private static void AssertLocalFunctionIdentityAndParents(GraphDocument graph, IReadOnlyCollection<GraphNode> declarations)
    {
        var localFunctions = declarations.Where(node => node.TypeId == "local-function").ToArray();
        var formatFunctions = localFunctions.Where(node => node.Label == "Format").ToArray();
        Assert.Equal(2, formatFunctions.Length);
        Assert.Equal(2, formatFunctions.Select(node => node.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(formatFunctions, node => node.Id.Contains("Describe()", StringComparison.Ordinal));
        Assert.Contains(formatFunctions, node => node.Id.Contains("Execute()", StringComparison.Ordinal));

        var normalize = Assert.Single(localFunctions, node => node.Label == "Normalize");
        Assert.Contains("Computed.accessor:PropertyGet.Normalize", normalize.Id);
        Assert.DoesNotContain("containerId", normalize.Attributes.Keys);
        Assert.DoesNotContain(graph.Links, link => link.TypeId == "contains" && link.Target == normalize.Id);
    }

    private static void AssertGeneratedCodeIsExcluded(GraphDocument graph, IReadOnlyCollection<GraphNode> declarations)
    {
        var excludedNames = new[]
        {
            "GeneratedSurface", "DesignerSurface", "GeneratedFileSurface", "AssemblyInfoSurface",
            "AssemblyAttributesSurface", "GeneratedAttributeSurface", "GeneratedAttributeMethod",
            "GeneratedAttributeMember"
        };
        Assert.DoesNotContain(declarations, node => excludedNames.Contains(node.Label, StringComparer.Ordinal));
        Assert.Contains(declarations, node => node.Label == "MixedGeneratedSurface");
        Assert.Contains(declarations, node => node.Label == "OwnMember");
        Assert.DoesNotContain(graph.Nodes, node => node.Attributes.TryGetValue("path", out var path)
            && path is string pathValue
            && (pathValue.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
                || pathValue.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
                || pathValue.Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
                || pathValue.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task OverloadsAndGenericTypeParametersHaveDistinctCanonicalIds()
    {
        var result = await AnalyzeReferenceMiniAsync();
        var overloads = result.Graph.Nodes
            .Where(node => node.TypeId == "method" && node.Label == "Convert")
            .ToArray();

        Assert.Equal(2, overloads.Length);
        Assert.NotEqual(overloads[0].Id, overloads[1].Id);
        Assert.All(overloads, node => Assert.Contains("Convert(", (string)node.Attributes["signature"]!));
        var generic = Assert.Single(result.Graph.Nodes, node => node.TypeId == "method" && node.Label == "Echo");
        Assert.Contains("Echo<T>", (string)generic.Attributes["signature"]!);
        var typeParameter = Assert.Single(result.Graph.Nodes, node => node.TypeId == "type-parameter"
            && node.Id.Contains("Echo<T>(T)::T#0", StringComparison.Ordinal));
        Assert.Contains("Echo<T>(T)::T#0", typeParameter.Id);
    }

    private static async Task<AnalysisResult> AnalyzeReferenceMiniAsync() =>
        await new WorkspaceAnalysis().AnalyzeAsync(CSharpReferenceMiniFixture.SolutionPath);

    private static bool IsDeclarationType(string typeId) => typeId is
        "class" or "interface" or "record" or "struct" or "enum" or "delegate" or
        "method" or "constructor" or "property" or "field" or "event" or "operator" or
        "local-function" or "type-parameter";

    private static void AssertHasRelativeSourcePosition(GraphNode node)
    {
        var source = Assert.IsType<SourcePosition>(node.Attributes["source"]);
        AssertHasRelativeSourcePosition(source);
    }

    private static void AssertHasRelativeSourcePosition(SourcePosition source)
    {
        Assert.DoesNotContain("\\", source.Path);
        Assert.DoesNotContain(":", source.Path);
        Assert.True(source.Line > 0);
        Assert.True(source.Column > 0);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "adapters", "csharp", "CodeVisualisierung.CSharp.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Das Repository wurde nicht gefunden.");
    }
}
