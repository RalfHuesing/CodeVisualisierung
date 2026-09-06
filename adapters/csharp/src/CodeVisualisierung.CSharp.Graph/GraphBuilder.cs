using CodeVisualisierung.CSharp.Contract;

namespace CodeVisualisierung.CSharp.Graph;

/// <summary>Deduplicates and deterministically orders contract graph data.</summary>
public sealed class GraphBuilder
{
    private readonly Dictionary<string, GraphNode> nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GraphLink> links = new(StringComparer.Ordinal);

    public void AddNode(GraphNode node) => nodes.TryAdd(node.Id, node);

    public void AddLink(string typeId, string source, string target)
    {
        var id = $"link:{typeId}:{source}:{target}";
        links.TryAdd(id, new GraphLink { Id = id, Source = source, Target = target, TypeId = typeId });
    }

    public GraphDocument Build()
    {
        var graph = new GraphDocument
        {
            NodeTypes = NodeTypes.ToList(),
            LinkTypes = LinkTypes.ToList(),
            Nodes = nodes.Values.OrderBy(node => node.Id, StringComparer.Ordinal).ToList(),
            Links = links.Values.OrderBy(link => link.Id, StringComparer.Ordinal).ToList()
        };
        GraphContractValidator.Validate(graph);
        return graph;
    }

    private static IReadOnlyList<GraphDefinition> NodeTypes { get; } =
    [
        new() { Id = "solution", Label = "Solution", Role = "container" },
        new() { Id = "project", Label = "Project", Role = "container" },
        new() { Id = "assembly", Label = "Assembly", Role = "container" },
        new() { Id = "module", Label = "Module", Role = "container" },
        new() { Id = "namespace", Label = "Namespace", Role = "container" },
        new() { Id = "file", Label = "File", Role = "container" },
        new() { Id = "class", Label = "Class", Role = "container" },
        new() { Id = "interface", Label = "Interface", Role = "container" },
        new() { Id = "record", Label = "Record", Role = "container" },
        new() { Id = "struct", Label = "Struct", Role = "container" },
        new() { Id = "enum", Label = "Enum", Role = "container" },
        new() { Id = "delegate", Label = "Delegate", Role = "container" },
        new() { Id = "method", Label = "Method", Role = "member" },
        new() { Id = "constructor", Label = "Constructor", Role = "member" },
        new() { Id = "property", Label = "Property", Role = "member" },
        new() { Id = "field", Label = "Field", Role = "member" },
        new() { Id = "event", Label = "Event", Role = "member" },
        new() { Id = "operator", Label = "Operator", Role = "member" },
        new() { Id = "local-function", Label = "Local function", Role = "member" },
        new() { Id = "type-parameter", Label = "Type parameter", Role = "member" }
    ];

    private static IReadOnlyList<GraphDefinition> LinkTypes { get; } =
    [
        new() { Id = "contains", Label = "Contains", Role = "containment" },
        new() { Id = "project-reference", Label = "Project reference", Role = "relation" },
        new() { Id = "references-assembly", Label = "Assembly reference", Role = "relation" }
    ];
}
