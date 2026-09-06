using CodeVisualisierung.CSharp.Contract;

namespace CodeVisualisierung.CSharp.Graph;

/// <summary>Deduplicates and deterministically orders contract graph data.</summary>
public sealed class GraphBuilder
{
    private readonly Dictionary<string, GraphNode> nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LinkEntry> links = new(StringComparer.Ordinal);

    public void AddNode(GraphNode node) => nodes.TryAdd(node.Id, node);

    public void SetNodeMetric(string nodeId, string metricName, double value)
    {
        if (nodes.TryGetValue(nodeId, out var node))
            node.Metrics[metricName] = value;
    }

    public void AddLink(string typeId, string source, string target)
    {
        var id = $"link:{typeId}:{source}:{target}";
        links.TryAdd(id, new LinkEntry(new GraphLink { Id = id, Source = source, Target = target, TypeId = typeId }));
    }

    public void AddDeclarationLink(string source, string target) => AddLink("declares", source, target);

    public void AddRelationLink(string typeId, string source, string target, int relationshipWeight)
    {
        var id = $"link:{typeId}:{source}:{target}";
        if (!links.TryGetValue(id, out var entry))
        {
            entry = new LinkEntry(new GraphLink
            {
                Id = id,
                Source = source,
                Target = target,
                TypeId = typeId,
                Weight = relationshipWeight,
                Metrics = new Dictionary<string, double>
                {
                    ["occurrences"] = 1,
                    ["relationshipWeight"] = relationshipWeight
                }
            });
            links.Add(id, entry);
            return;
        }

        var metrics = new Dictionary<string, double>(entry.Link.Metrics, StringComparer.Ordinal)
        {
            ["occurrences"] = entry.Link.Metrics.GetValueOrDefault("occurrences") + 1,
            ["relationshipWeight"] = entry.Link.Metrics.GetValueOrDefault("relationshipWeight") + relationshipWeight
        };
        var updatedLink = new GraphLink
        {
            Id = entry.Link.Id,
            Source = entry.Link.Source,
            Target = entry.Link.Target,
            TypeId = entry.Link.TypeId,
            Directed = entry.Link.Directed,
            Weight = metrics["relationshipWeight"],
            Summary = entry.Link.Summary,
            DerivedFrom = entry.Link.DerivedFrom,
            Metrics = metrics,
            Attributes = entry.Link.Attributes
        };
        links[id] = entry with { Link = updatedLink };
    }

    public GraphDocument Build()
    {
        var graph = new GraphDocument
        {
            NodeTypes = NodeTypes.ToList(),
            LinkTypes = LinkTypes.ToList(),
            MetricDefinitions = GraphContractDefinitions.MetricDefinitions(),
            Facets = GraphContractDefinitions.Facets().ToList(),
            FilterSources = GraphContractDefinitions.FilterSources().ToList(),
            ViewProfiles = GraphContractDefinitions.ViewProfiles().ToList(),
            LayoutProfiles = GraphContractDefinitions.LayoutProfiles().ToList(),
            Projections = GraphContractDefinitions.Projections().ToList(),
            ContainmentRules = GraphContractDefinitions.ContainmentRules().ToList(),
            Hierarchy = GraphContractDefinitions.Hierarchy(),
            Nodes = nodes.Values.OrderBy(node => node.Id, StringComparer.Ordinal).ToList(),
            Links = links.Values.Select(entry => entry.Link).OrderBy(link => link.Id, StringComparer.Ordinal).ToList()
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
        new() { Id = "declares", Label = "Declares", Role = "containment" },
        new() { Id = "calls", Label = "Calls", Role = "relation" },
        new() { Id = "constructs", Label = "Constructs", Role = "relation" },
        new() { Id = "inherits", Label = "Inherits", Role = "relation" },
        new() { Id = "implements", Label = "Implements", Role = "relation" },
        new() { Id = "overrides", Label = "Overrides", Role = "relation" },
        new() { Id = "reads", Label = "Reads", Role = "relation" },
        new() { Id = "writes", Label = "Writes", Role = "relation" },
        new() { Id = "uses-type", Label = "Uses type", Role = "relation" },
        new() { Id = "returns-type", Label = "Returns type", Role = "relation" },
        new() { Id = "parameter-type", Label = "Parameter type", Role = "relation" },
        new() { Id = "project-reference", Label = "Project reference", Role = "relation" },
        new() { Id = "references-assembly", Label = "Assembly reference", Role = "relation" },
        new() { Id = "tests", Label = "Tests", Role = "relation" },
        new() { Id = "summary-calls", Label = "Summary calls", Role = "summary" },
        new() { Id = "summary-depends-on", Label = "Summary dependency", Role = "summary" },
        new() { Id = "summary-references", Label = "Summary reference", Role = "summary" }
    ];

    private sealed record LinkEntry(GraphLink Link);
}
