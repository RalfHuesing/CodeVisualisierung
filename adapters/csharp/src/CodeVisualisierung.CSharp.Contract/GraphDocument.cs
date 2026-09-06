using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema;

namespace CodeVisualisierung.CSharp.Contract;

/// <summary>Serializable graph-universe document.</summary>
public sealed class GraphDocument
{
    public GraphFormat Format { get; init; } = new();
    public List<GraphDefinition> NodeTypes { get; init; } = [];
    public List<GraphDefinition> LinkTypes { get; init; } = [];
    public Dictionary<string, GraphMetricDefinition> MetricDefinitions { get; init; } = [];
    public List<GraphFacet> Facets { get; init; } = [];
    public List<GraphFilterSource> FilterSources { get; init; } = [];
    public List<GraphViewProfile> ViewProfiles { get; init; } = [];
    public List<GraphLayoutProfile> LayoutProfiles { get; init; } = [];
    public List<GraphProjection> Projections { get; init; } = [];
    public List<GraphContainmentRule> ContainmentRules { get; init; } = [];
    public GraphHierarchy Hierarchy { get; init; } = new();
    public List<GraphNode> Nodes { get; init; } = [];
    public List<GraphLink> Links { get; init; } = [];
}

/// <summary>Versioned graph contract identifier.</summary>
public sealed class GraphFormat
{
    public string Name { get; init; } = "graph-universe";
    public string Version { get; init; } = "1.0";
}

/// <summary>Declarative node or link type definition.</summary>
public sealed class GraphDefinition
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public string? Role { get; init; }
    public string? VisualToken { get; init; }
    public string? VisualRole { get; init; }
    public double? BaseSize { get; init; }
    public bool? Directed { get; init; }
}

/// <summary>Explains a named numeric node or link metric.</summary>
public sealed class GraphMetricDefinition
{
    public string? Label { get; init; }
    public string? Unit { get; init; }
    public string ValueType { get; init; } = "number";
    public string? Description { get; init; }
    public string? ValueKind { get; init; }
    public double[]? Range { get; init; }
}

/// <summary>Declares a graph facet and its source field.</summary>
public sealed class GraphFacet
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public GraphFacetSource? Source { get; init; }
}

/// <summary>Source field used by a facet.</summary>
public sealed class GraphFacetSource
{
    public required string Scope { get; init; }
    public required string Field { get; init; }
}

/// <summary>Declares a selectable source for a facet.</summary>
public sealed class GraphFilterSource
{
    public required string Id { get; init; }
    public required string FacetId { get; init; }
    public string? Label { get; init; }
}

/// <summary>Declares a visible graph detail level.</summary>
public sealed class GraphViewProfile
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public int? DetailLevel { get; init; }
    public List<string> VisibleNodeTypes { get; init; } = [];
    public List<string> VisibleLinkTypes { get; init; } = [];
    public string? NodeMetric { get; init; }
    public string? LinkMetric { get; init; }
    public string? LayoutProfileId { get; init; }
}

/// <summary>Declares source-neutral spatial constraints.</summary>
public sealed class GraphLayoutProfile
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public string GroupField { get; init; } = "groupId";
    public double? GroupDistance { get; init; }
    public double? DefaultDistance { get; init; }
    public List<GraphContainmentDistance> ContainmentDistances { get; init; } = [];
}

/// <summary>Declares a distance for one parent-child type pair.</summary>
public sealed class GraphContainmentDistance
{
    public required string ParentTypeId { get; init; }
    public required string ChildTypeId { get; init; }
    public required double Distance { get; init; }
}

/// <summary>Declares a projection between detail profiles.</summary>
public sealed class GraphProjection
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public string? FromProfile { get; init; }
    public string? ToProfile { get; init; }
    public string? LinkTypeId { get; init; }
}

/// <summary>Declares an allowed containment edge.</summary>
public sealed class GraphContainmentRule
{
    public required string Id { get; init; }
    public required string LinkTypeId { get; init; }
    public string? ParentTypeId { get; init; }
    public string? ChildTypeId { get; init; }
    public int? MaxParents { get; init; }
    public bool? Acyclic { get; init; }
}

/// <summary>Declares the graph containment hierarchy.</summary>
public sealed class GraphHierarchy
{
    public List<string> ContainmentLinkTypes { get; init; } = [];
    public int? MaxParents { get; init; }
    public bool? Acyclic { get; init; }
}

/// <summary>Flat graph node with a stable identifier.</summary>
public sealed class GraphNode
{
    public required string Id { get; init; }
    public required string TypeId { get; init; }
    public required string Label { get; init; }
    public string? GroupId { get; init; }
    public Dictionary<string, double> Metrics { get; init; } = [];
    public Dictionary<string, object?> Attributes { get; init; } = [];
}

/// <summary>Directed graph relationship between two nodes.</summary>
public sealed class GraphLink
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public required string Target { get; init; }
    public required string TypeId { get; init; }
    public bool Directed { get; init; } = true;
    public double? Weight { get; init; }
    public bool? Summary { get; init; }
    public List<string>? DerivedFrom { get; init; }
    public Dictionary<string, double> Metrics { get; init; } = [];
    public Dictionary<string, object?> Attributes { get; init; } = [];
}

/// <summary>Deterministic JSON serialization for graph documents.</summary>
public static class GraphJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(GraphDocument graph) => JsonSerializer.Serialize(graph, Options) + Environment.NewLine;
}

/// <summary>Validates the contract invariants required by the adapter.</summary>
public static class GraphContractValidator
{
    public static void Validate(GraphDocument graph)
    {
        ValidateFormat(graph);
        var nodeIds = ValidateNodeIds(graph);
        var nodeTypes = ValidateDefinitions(graph.NodeTypes, "Node");
        var linkTypes = ValidateDefinitions(graph.LinkTypes, "Link");
        ValidateNodes(graph.Nodes, nodeTypes);
        ValidateLinks(graph.Links, nodeIds, linkTypes);
    }

    private static void ValidateFormat(GraphDocument graph)
    {
        if (graph.Format.Name != "graph-universe" || graph.Format.Version != "1.0")
            throw new InvalidOperationException("Das Graphformat muss graph-universe 1.0 sein.");
    }

    private static HashSet<string> ValidateNodeIds(GraphDocument graph)
    {
        var ids = graph.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != graph.Nodes.Count || ids.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Die Graph-Nodes benötigen eindeutige, nichtleere IDs.");
        return ids;
    }

    private static HashSet<string> ValidateDefinitions(IEnumerable<GraphDefinition> definitions, string label)
    {
        var definitionList = definitions.ToArray();
        var ids = definitionList.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != definitionList.Length || ids.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{label}-Definitionen benötigen nichtleere IDs.");
        return ids;
    }

    private static void ValidateNodes(IEnumerable<GraphNode> nodes, HashSet<string> nodeTypes)
    {
        foreach (var node in nodes)
        {
            if (!nodeTypes.Contains(node.TypeId))
                throw new InvalidOperationException($"Unbekannter Node-Typ: {node.TypeId}.");
            ValidateMetrics(node.Metrics, $"Node {node.Id}");
        }
    }

    private static void ValidateLinks(IEnumerable<GraphLink> links, HashSet<string> nodeIds, HashSet<string> linkTypes)
    {
        var linkList = links.ToArray();
        var ids = linkList.Select(link => link.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != linkList.Length || ids.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Graph-Links benötigen eindeutige, nichtleere IDs.");
        foreach (var link in linkList)
        {
            if (!nodeIds.Contains(link.Source) || !nodeIds.Contains(link.Target))
                throw new InvalidOperationException($"Ungültiger Graph-Link: {link.Id}.");
            if (!linkTypes.Contains(link.TypeId))
                throw new InvalidOperationException($"Unbekannter Link-Typ: {link.TypeId}.");
            ValidateMetrics(link.Metrics, $"Link {link.Id}");
            if (link.DerivedFrom?.Any(derivedId => !ids.Contains(derivedId)) == true)
                throw new InvalidOperationException($"Summary-Link {link.Id} verweist auf eine unbekannte Detailbeziehung.");
        }
    }

    private static void ValidateMetrics(IReadOnlyDictionary<string, double> metrics, string owner)
    {
        if (metrics.Any(metric => double.IsNaN(metric.Value) || double.IsInfinity(metric.Value)))
            throw new InvalidOperationException($"{owner} enthält eine nicht endliche Metrik.");
    }
}

/// <summary>Validates serialized output against the repository's sole schema source.</summary>
public static class GraphSchemaValidator
{
    /// <summary>Loads and applies the supplied graph-universe schema.</summary>
    public static async Task ValidateAsync(string json, string schemaPath, CancellationToken cancellationToken = default)
    {
        var schemaText = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        var compatibleSchemaText = schemaText.Replace("#/$defs/", "#/definitions/", StringComparison.Ordinal)
            .Replace("\"$defs\"", "\"definitions\"", StringComparison.Ordinal);
        var schema = await JsonSchema.FromJsonAsync(compatibleSchemaText, cancellationToken);
        var errors = schema.Validate(json);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Die Graphausgabe verletzt das gemeinsame Schema: {errors.First().Path}.");
        }
    }
}
