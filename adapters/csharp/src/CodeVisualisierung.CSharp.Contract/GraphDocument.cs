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
}

/// <summary>Flat graph node with a stable identifier.</summary>
public sealed class GraphNode
{
    public required string Id { get; init; }
    public required string TypeId { get; init; }
    public required string Label { get; init; }
    public string? GroupId { get; init; }
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
        var ids = definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{label}-Definitionen benötigen nichtleere IDs.");
        return ids;
    }

    private static void ValidateNodes(IEnumerable<GraphNode> nodes, HashSet<string> nodeTypes)
    {
        foreach (var node in nodes)
            if (!nodeTypes.Contains(node.TypeId))
                throw new InvalidOperationException($"Unbekannter Node-Typ: {node.TypeId}.");
    }

    private static void ValidateLinks(IEnumerable<GraphLink> links, HashSet<string> nodeIds, HashSet<string> linkTypes)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            if (!ids.Add(link.Id) || !nodeIds.Contains(link.Source) || !nodeIds.Contains(link.Target))
                throw new InvalidOperationException($"Ungültiger Graph-Link: {link.Id}.");
            if (!linkTypes.Contains(link.TypeId))
                throw new InvalidOperationException($"Unbekannter Link-Typ: {link.TypeId}.");
        }
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
