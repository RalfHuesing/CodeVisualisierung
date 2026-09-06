using System.Text.Json.Nodes;
using CodeVisualisierung.CSharp.Analysis;
using CodeVisualisierung.CSharp.Contract;
using CodeVisualisierung.CSharp.Fixtures;
using CodeVisualisierung.CSharp.Graph;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests;

public sealed class MetricsAndProjectionTests
{
    [Fact]
    public async Task ReferenceMiniEmitsRawMetricsAndNormalizedScoresSeparately()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;

        Assert.Equal("raw", graph.MetricDefinitions["loc"].ValueKind);
        Assert.Equal("normalized-score", graph.MetricDefinitions["importance"].ValueKind);
        Assert.Equal("normalized-score", graph.MetricDefinitions["footprint"].ValueKind);
        Assert.Equal("overview", graph.ViewProfiles[0].Id);
        Assert.Equal(0, graph.ViewProfiles[0].DetailLevel);

        var measuredNodes = graph.Nodes.Where(node => node.Metrics.ContainsKey("importance")).ToArray();
        Assert.NotEmpty(measuredNodes);
        Assert.All(measuredNodes, node =>
        {
            Assert.InRange(node.Metrics["importance"], 0, 1);
            Assert.InRange(node.Metrics["footprint"], 0, 1);
            Assert.True(node.Metrics.ContainsKey("fanIn"));
            Assert.True(node.Metrics.ContainsKey("fanOut"));
            Assert.True(node.Metrics.ContainsKey("weightedFanIn"));
            Assert.True(node.Metrics.ContainsKey("weightedFanOut"));
            Assert.True(node.Metrics.ContainsKey("pageRank"));
            Assert.DoesNotContain(node.Metrics.Values, value => double.IsNaN(value) || double.IsInfinity(value));
        });

        var classify = Assert.Single(graph.Nodes, node => node.TypeId == "method" && node.Label == "Classify");
        Assert.Equal(4, classify.Metrics["cyclomaticComplexity"]);
        Assert.True(classify.Metrics["loc"] >= 5);
        var classifyWithLocal = Assert.Single(graph.Nodes, node => node.TypeId == "method" && node.Label == "ClassifyWithLocal");
        var localFunction = Assert.Single(graph.Nodes, node => node.TypeId == "local-function" && node.Label == "IsPositive");
        Assert.Equal(2, classifyWithLocal.Metrics["cyclomaticComplexity"]);
        Assert.Equal(2, localFunction.Metrics["cyclomaticComplexity"]);

        Assert.DoesNotContain(graph.Nodes, node => node.Metrics.ContainsKey("importance") && node.TypeId is "file" or "project");
    }

    [Fact]
    public async Task PartialTypeFootprintUsesAllPhysicalDeclarationFilesWithoutDuplicatingTheType()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;
        var partial = Assert.Single(graph.Nodes, node => node.TypeId == "class" && node.Label == "PartialCoordinator");

        Assert.Equal(1, graph.Nodes.Count(node => node.Id == partial.Id));
        Assert.Equal(2, partial.Metrics["partialDeclarationCount"]);
        Assert.Equal(2, partial.Metrics["fileCount"]);
        Assert.Equal(2, ((string[])partial.Attributes["declarationFiles"]!).Length);
        Assert.True(partial.Metrics["loc"] > 0);
        Assert.InRange(partial.Metrics["footprint"], 0, 1);
        var partialFiles = graph.Nodes.Where(node => node.TypeId == "file"
            && node.Attributes.TryGetValue("path", out var path)
            && path is string filePath
            && filePath.Contains("PartialCoordinator", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, partialFiles.Length);
        Assert.All(partialFiles, file => Assert.InRange(file.Metrics["footprint"], 0, 1));

        var nonPartial = Assert.Single(graph.Nodes, node => node.TypeId == "class" && node.Label == "Greeter");
        Assert.DoesNotContain("partialDeclarationCount", nonPartial.Metrics.Keys);
        Assert.DoesNotContain("partialDeclarationCount", nonPartial.Attributes.Keys);
    }

    [Fact]
    public async Task SummaryLinksExposeOriginLevelsAndAuditableDetailRelationships()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;
        var detailLinks = graph.Links.Where(link => link.Summary != true).ToDictionary(link => link.Id, StringComparer.Ordinal);
        var summaries = graph.Links.Where(link => link.Summary == true).ToArray();

        Assert.NotEmpty(summaries);
        Assert.All(summaries, summary =>
        {
            Assert.NotEmpty(summary.DerivedFrom!);
            Assert.All(summary.DerivedFrom!, detailId => Assert.Contains(detailId, detailLinks));
            Assert.True(summary.Metrics["occurrences"] > 0);
            Assert.True(summary.Metrics["relationshipWeight"] > 0);
            var aggregation = Assert.IsType<Dictionary<string, object?>>(summary.Attributes["aggregation"]);
            Assert.Equal("detail-relations", aggregation["origin"]);
            Assert.Equal(aggregation["sourceLevel"], aggregation["targetLevel"]);
        });

        Assert.Contains(summaries, summary => summary.TypeId == "summary-calls");
        Assert.Contains(graph.Projections, projection => projection.LinkTypeId == "summary-calls");
        Assert.Contains(graph.Projections, projection => projection is
        {
            FromProfile: "architecture",
            ToProfile: "overview",
            LinkTypeId: "summary-references"
        });
        Assert.Contains(summaries, summary => summary.TypeId == "summary-depends-on"
            && summary.DerivedFrom!.Any(detailId => detailLinks[detailId].TypeId is "uses-type" or "returns-type" or "parameter-type" or "constructs"));
        var assemblyReferences = detailLinks.Values.Where(link => link.TypeId == "references-assembly").ToArray();
        var summaryReferences = summaries.Where(link => link.TypeId == "summary-references").ToArray();
        Assert.NotEmpty(assemblyReferences);
        Assert.NotEmpty(summaryReferences);
        Assert.All(assemblyReferences, link =>
        {
            Assert.Equal(1, link.Metrics["occurrences"]);
            Assert.Equal(1, link.Metrics["relationshipWeight"]);
            Assert.Equal("assembly", graph.Nodes.Single(node => node.Id == link.Source).TypeId);
            Assert.Equal("assembly", graph.Nodes.Single(node => node.Id == link.Target).TypeId);
        });
        Assert.All(summaryReferences, summary =>
        {
            Assert.True(summary.Directed);
            Assert.Equal("assembly", graph.Nodes.Single(node => node.Id == summary.Source).TypeId);
            Assert.Equal("assembly", graph.Nodes.Single(node => node.Id == summary.Target).TypeId);
            Assert.Equal(summary.Source, graph.Links.Single(link => link.Id == summary.DerivedFrom!.Single()).Source);
            Assert.Equal(summary.Target, graph.Links.Single(link => link.Id == summary.DerivedFrom!.Single()).Target);
            Assert.All(summary.DerivedFrom!, detailId => Assert.Equal("references-assembly", detailLinks[detailId].TypeId));
            Assert.Equal(summary.DerivedFrom!.Count, summary.Metrics["occurrences"]);
            Assert.Equal(summary.DerivedFrom!.Count, summary.Metrics["relationshipWeight"]);
            var aggregation = Assert.IsType<Dictionary<string, object?>>(summary.Attributes["aggregation"]);
            Assert.Equal("assembly", aggregation["sourceLevel"]);
            Assert.Equal("assembly", aggregation["targetLevel"]);
        });
        Assert.True(graph.Links.Zip(graph.Links.Skip(1)).All(pair => string.CompareOrdinal(pair.First.Id, pair.Second.Id) <= 0));
    }

    [Fact]
    public async Task ContractDriftTestFailsForIntentionalSliceFiveContractChanges()
    {
        var graph = (await AnalyzeReferenceMiniAsync()).Graph;
        var schemaPath = Path.Combine(FindRepositoryRoot(), "contracts", "graph-universe", "schema", "graph-universe.schema.json");
        var invalidRange = JsonNode.Parse(GraphJson.Serialize(graph))!.AsObject();
        invalidRange["metricDefinitions"]!["importance"]!["range"] = new JsonArray(0, 1, 2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => GraphSchemaValidator.ValidateAsync(invalidRange.ToJsonString(), schemaPath));

        var invalidValueKind = JsonNode.Parse(GraphJson.Serialize(graph))!.AsObject();
        invalidValueKind["metricDefinitions"]!["importance"]!["valueKind"] = "local-scale";
        await Assert.ThrowsAsync<InvalidOperationException>(() => GraphSchemaValidator.ValidateAsync(invalidValueKind.ToJsonString(), schemaPath));

        var invalidProjection = JsonNode.Parse(GraphJson.Serialize(graph))!.AsObject();
        invalidProjection["projections"]!.AsArray()[0]!.AsObject().Remove("id");
        await Assert.ThrowsAsync<InvalidOperationException>(() => GraphSchemaValidator.ValidateAsync(invalidProjection.ToJsonString(), schemaPath));
    }

    [Fact]
    public void MetricProjectionPhasePreservesA180KLocGraphInvariant()
    {
        var smallGraph = CreateScalableGraph(900);
        var largeGraph = CreateScalableGraph(1_800);
        var smallStats = GraphMetricsAndProjections.Apply(smallGraph);
        var largeStats = GraphMetricsAndProjections.Apply(largeGraph);

        Assert.Equal(90_000, smallGraph.Nodes.Sum(node => node.Metrics["loc"]));
        Assert.Equal(180_000, largeGraph.Nodes.Sum(node => node.Metrics["loc"]));
        Assert.Equal(smallStats.NormalizationValueScans * 2, largeStats.NormalizationValueScans);
        Assert.Equal(900, smallGraph.Links.Count(link => link.TypeId == "calls"));
        Assert.Equal(1_800, largeGraph.Links.Count(link => link.TypeId == "calls"));
        Assert.All(largeGraph.Nodes, node => Assert.InRange(node.Metrics["importance"], 0, 1));
    }

    private static GraphDocument CreateScalableGraph(int nodeCount) => new()
    {
        Nodes = Enumerable.Range(0, nodeCount)
            .Select(index => new GraphNode
            {
                Id = $"method:{index:D4}", TypeId = "method", Label = $"Method {index}",
                Metrics = new Dictionary<string, double> { ["loc"] = 100 + (index % 2 == 0 ? 1 : -1) }
            }).ToList(),
        Links = Enumerable.Range(0, nodeCount)
            .Select(index => new GraphLink
            {
                Id = $"link:calls:method:{index:D4}:method:{(index + 1) % nodeCount:D4}", Source = $"method:{index:D4}",
                Target = $"method:{(index + 1) % nodeCount:D4}", TypeId = "calls", Weight = 1,
                Metrics = new Dictionary<string, double> { ["occurrences"] = 1, ["relationshipWeight"] = 1 }
            }).ToList()
    };

    private static async Task<AnalysisResult> AnalyzeReferenceMiniAsync() =>
        await new WorkspaceAnalysis().AnalyzeAsync(CSharpReferenceMiniFixture.SolutionPath);

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
