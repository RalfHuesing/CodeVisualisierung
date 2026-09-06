using CodeVisualisierung.CSharp.Analysis;
using CodeVisualisierung.CSharp.Contract;
using CodeVisualisierung.CSharp.Fixtures;
using CodeVisualisierung.CSharp.Graph;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class SemanticRelationTests
{
    [Fact]
    public async Task ReferenceMiniEmitsAllV1SemanticRelationsWithCorrectDirection()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;

        AssertRelationTypes(graph);
        AssertRelation(graph, "inherits", "DerivedProcessor", "BaseProcessor");
        AssertRelation(graph, "implements", "DerivedProcessor", "IProcessor");
        AssertRelation(graph, "inherits", "IChildProcessor", "IParentProcessor");
        AssertRelation(graph, "implements", "InterfaceChildProcessor", "IChildProcessor");
        Assert.DoesNotContain(graph.Links, link => link.TypeId == "inherits"
            && NodeMatches(graph, link.Source, "InterfaceChildProcessor")
            && NodeMatches(graph, link.Target, "IChildProcessor"));
        AssertRelation(graph, "overrides", "DerivedProcessor.Process", "BaseProcessor.Process");
        AssertRelation(graph, "overrides", "EventDerived.Changed", "EventBase.Changed");
        AssertRelation(graph, "calls", "SemanticUseSite.Execute", "OverloadAndGeneric.Convert");
        AssertRelation(graph, "constructs", "SemanticUseSite.Execute", "DerivedProcessor");
        AssertRelation(graph, "reads", "PartialCoordinator.Increment", "PartialCoordinator.increments");
        AssertRelation(graph, "writes", "PartialCoordinator.Increment", "PartialCoordinator.Value");
        AssertRelation(graph, "uses-type", "SemanticUseSite.Execute", "StringTransformer");
        AssertRelation(graph, "returns-type", "OverloadAndGeneric.Echo", "OverloadAndGeneric.Echo");
        AssertRelation(graph, "parameter-type", "GreetingService", "IGreeter");
        AssertRelation(graph, "tests", "GreetingServiceTests.Creates_greeting_through_project_reference", "GreetingService.CreateGreeting");
        AssertRelation(graph, "declares", "solution:root", "CSharpReferenceMini.Application");
        AssertRelation(graph, "declares", "SemanticSurface.cs", "DerivedProcessor");
        AssertRelation(graph, "declares", "CSharpReferenceMini.Application", "DerivedProcessor");
        AssertRelation(graph, "declares", "DerivedProcessor", "DerivedProcessor.Process");

        AssertAccessMode(graph, "SemanticUseSite.UseOut", reads: false, writes: true);
        AssertAccessMode(graph, "SemanticUseSite.UseRef", reads: true, writes: true);
        AssertAccessMode(graph, "SemanticUseSite.UseIn", reads: true, writes: false);

        var nodeIds = graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        Assert.All(graph.Links, link =>
        {
            Assert.Contains(link.Source, nodeIds);
            Assert.Contains(link.Target, nodeIds);
            Assert.True(link.Directed);
        });
    }

    [Fact]
    public async Task ReferenceMiniDropsExternalAndUnresolvedTargetsWithoutDanglingLinks()
    {
        var result = await AnalyzeReferenceMiniAsync();

        Assert.True(result.Summary.ExternalDropped > 0);
        Assert.Equal(15, result.Summary.Unresolved);
        Assert.DoesNotContain(result.Graph.Nodes, node => node.Attributes.Values
            .OfType<string>().Any(value => value.StartsWith("global::System.", StringComparison.Ordinal)
                || value.StartsWith("System.", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GenericTypeParametersResolveToTheirOwnOwnerAndOrdinal()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;
        var first = FindNode(graph, "method", "OverloadAndGeneric.First");
        var second = FindNode(graph, "method", "OverloadAndGeneric.Second");
        var firstTypeParameter = FindNode(graph, "type-parameter", "OverloadAndGeneric.First");
        var secondTypeParameter = FindNode(graph, "type-parameter", "OverloadAndGeneric.Second");

        AssertRelationByIds(graph, "returns-type", first.Id, firstTypeParameter.Id);
        AssertRelationByIds(graph, "parameter-type", first.Id, firstTypeParameter.Id);
        AssertRelationByIds(graph, "returns-type", second.Id, secondTypeParameter.Id);
        AssertRelationByIds(graph, "parameter-type", second.Id, secondTypeParameter.Id);
        Assert.DoesNotContain(graph.Links, link => link.TypeId == "returns-type"
            && link.Source == first.Id && link.Target == secondTypeParameter.Id);
        Assert.DoesNotContain(graph.Links, link => link.TypeId == "returns-type"
            && link.Source == second.Id && link.Target == firstTypeParameter.Id);
    }

    [Fact]
    public async Task TestLinksOnlyStartAtRecognizedTestMethodsAndTargetProductNodes()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;
        var testLinks = graph.Links.Where(link => link.TypeId == "tests").ToArray();

        Assert.NotEmpty(testLinks);
        Assert.All(testLinks, link =>
        {
            Assert.True(NodeMatches(graph, link.Source, "GreetingServiceTests.Creates_greeting_through_project_reference"));
            Assert.DoesNotContain(graph.Nodes.First(node => node.Id == link.Target).Attributes.Values,
                value => value is string text && text.Contains("CSharpReferenceMini.Tests", StringComparison.Ordinal));
        });
        Assert.DoesNotContain(testLinks, link => NodeMatches(graph, link.Source, "CreateGreetingThroughHelper"));
        Assert.All(testLinks, link => Assert.Equal("method", graph.Nodes.First(node => node.Id == link.Source).TypeId));
    }

    [Fact]
    public async Task NestedGenericTypeUsesCountSemanticOccurrencesWithoutAstOverlap()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;
        var source = FindNode(graph, "method", "SemanticUseSite.UseNestedType");
        var box = Assert.Single(graph.Nodes.Where(node => node.TypeId == "class" && node.Label == "Box"));
        var link = Assert.Single(graph.Links.Where(item => item.TypeId == "uses-type"
            && item.Source == source.Id && item.Target == box.Id));

        Assert.Contains("Box<T>", (string)box.Attributes["signature"]!);
        Assert.Equal(2, link.Metrics["occurrences"]);
        Assert.Equal(2, link.Metrics["relationshipWeight"]);
    }

    [Fact]
    public async Task SummaryCountersFollowTheCurrentWorkspacePolicy()
    {
        var result = await AnalyzeReferenceMiniAsync();

        Assert.Equal(3, result.Summary.ProjectsLoaded);
        Assert.Equal(3, result.Summary.ProjectsAnalyzed);
        Assert.Equal(0, result.Summary.ProjectsSkipped);
        Assert.Equal(0, result.Summary.ProjectsFailed);
        Assert.Equal(0, result.Summary.Warnings);
        Assert.Equal(0, result.Summary.Errors);
    }

    [Fact]
    public async Task ReferenceMiniOutputIsBytewiseDeterministic()
    {
        var first = GraphJson.Serialize((await AnalyzeReferenceMiniAsync()).Graph);
        var second = GraphJson.Serialize((await AnalyzeReferenceMiniAsync()).Graph);

        Assert.Equal(first, second);
    }

    [Fact]
    public void RelationLinksAggregateRepeatedOccurrencesDeterministically()
    {
        var builder = new GraphBuilder();
        builder.AddNode(new GraphNode { Id = "source", TypeId = "method", Label = "Source" });
        builder.AddNode(new GraphNode { Id = "target", TypeId = "method", Label = "Target" });
        builder.AddRelationLink("calls", "source", "target", 3);
        builder.AddRelationLink("calls", "source", "target", 3);
        builder.AddDeclarationLink("source", "target");

        var link = Assert.Single(builder.Build().Links, item => item.TypeId == "calls");
        Assert.Equal(2, link.Metrics["occurrences"]);
        Assert.Equal(6, link.Metrics["relationshipWeight"]);
        var declaration = Assert.Single(builder.Build().Links, item => item.TypeId == "declares");
        Assert.Equal("source", declaration.Source);
        Assert.Equal("target", declaration.Target);
    }

    private static void AssertRelationTypes(GraphDocument graph)
    {
        var expected = new[]
        {
            "declares", "calls", "constructs", "inherits", "implements", "overrides", "reads", "writes",
            "uses-type", "returns-type", "parameter-type", "references-assembly", "project-reference", "tests"
        };
        Assert.All(expected, typeId => Assert.Contains(graph.Links, link => link.TypeId == typeId));
    }

    private static void AssertRelation(GraphDocument graph, string typeId, string sourcePart, string targetPart)
    {
        Assert.Contains(graph.Links, link => link.TypeId == typeId
            && NodeMatches(graph, link.Source, sourcePart)
            && NodeMatches(graph, link.Target, targetPart));
    }

    private static void AssertRelationByIds(GraphDocument graph, string typeId, string sourceId, string targetId) =>
        Assert.Contains(graph.Links, link => link.TypeId == typeId
            && link.Source == sourceId && link.Target == targetId);

    private static void AssertAccessMode(GraphDocument graph, string sourcePart, bool reads, bool writes)
    {
        var readsLink = graph.Links.Any(link => link.TypeId == "reads"
            && NodeMatches(graph, link.Source, sourcePart)
            && NodeMatches(graph, link.Target, "MutableCounter.Value"));
        var writesLink = graph.Links.Any(link => link.TypeId == "writes"
            && NodeMatches(graph, link.Source, sourcePart)
            && NodeMatches(graph, link.Target, "MutableCounter.Value"));
        Assert.Equal(reads, readsLink);
        Assert.Equal(writes, writesLink);
    }

    private static GraphNode FindNode(GraphDocument graph, string typeId, string text) =>
        Assert.Single(graph.Nodes.Where(node => node.TypeId == typeId
            && (node.Id.Contains(text, StringComparison.Ordinal)
                || node.Attributes.Values.OfType<string>().Any(value => value.Contains(text, StringComparison.Ordinal)))));

    private static bool NodeMatches(GraphDocument graph, string nodeId, string text) =>
        graph.Nodes.First(node => node.Id == nodeId).Id.Contains(text, StringComparison.Ordinal)
        || graph.Nodes.First(node => node.Id == nodeId).Attributes.Values
            .OfType<string>().Any(value => value.Contains(text, StringComparison.Ordinal));

    private static async Task<AnalysisResult> AnalyzeReferenceMiniAsync() =>
        await new WorkspaceAnalysis().AnalyzeAsync(CSharpReferenceMiniFixture.SolutionPath);
}
