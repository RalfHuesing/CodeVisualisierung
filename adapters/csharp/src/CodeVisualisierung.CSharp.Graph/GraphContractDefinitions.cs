using CodeVisualisierung.CSharp.Contract;

namespace CodeVisualisierung.CSharp.Graph;

internal static class GraphContractDefinitions
{
    public static Dictionary<string, GraphMetricDefinition> MetricDefinitions() => new(StringComparer.Ordinal)
    {
        ["loc"] = Raw("Lines of code", "lines", "Non-empty source lines in the declaration or document scope."),
        ["cyclomaticComplexity"] = Raw("Cyclomatic complexity", "branches", "One plus syntactically recognized branching points."),
        ["fanIn"] = Raw("Fan-in", "relationships", "Distinct incoming semantic relationships."),
        ["fanOut"] = Raw("Fan-out", "relationships", "Distinct outgoing semantic relationships."),
        ["weightedFanIn"] = Raw("Weighted fan-in", "relationship-weight", "Incoming relationship weight including occurrences."),
        ["weightedFanOut"] = Raw("Weighted fan-out", "relationship-weight", "Outgoing relationship weight including occurrences."),
        ["pageRank"] = Raw("PageRank", "score", "Deterministic structural relevance before importance blending."),
        ["importance"] = new() { Label = "Importance", Unit = "score", Description = "Normalized structural relevance for one node level.", ValueKind = "normalized-score", Range = [0, 1] },
        ["footprint"] = new() { Label = "Physical footprint", Unit = "score", Description = "Normalized physical size and fragmentation score for one node level.", ValueKind = "normalized-score", Range = [0, 1] },
        ["partialDeclarationCount"] = Raw("Partial declarations", "declarations", "Number of source declarations merged into one type node."),
        ["fileCount"] = Raw("Files", "files", "Distinct source files represented by a container."),
        ["typeCount"] = Raw("Types", "types", "Contained type nodes."),
        ["memberCount"] = Raw("Members", "members", "Contained member nodes."),
        ["occurrences"] = Raw("Occurrences", "occurrences", "Repeated detail relationships represented by one link."),
        ["relationshipWeight"] = Raw("Relationship weight", "relationship-weight", "Sum of detail relationship weights.")
    };

    public static IReadOnlyList<GraphFacet> Facets() =>
    [
        new() { Id = "node-type", Label = "Node type", Source = new() { Scope = "node", Field = "typeId" } },
        new() { Id = "project", Label = "Project", Source = new() { Scope = "node", Field = "groupId" } },
        new() { Id = "link-type", Label = "Link type", Source = new() { Scope = "link", Field = "typeId" } }
    ];

    public static IReadOnlyList<GraphFilterSource> FilterSources() =>
    [
        new() { Id = "node-type", FacetId = "node-type", Label = "Node type" },
        new() { Id = "project", FacetId = "project", Label = "Project" },
        new() { Id = "link-type", FacetId = "link-type", Label = "Link type" }
    ];

    public static IReadOnlyList<GraphViewProfile> ViewProfiles() =>
    [
        new()
        {
            Id = "overview", Label = "Overview", DetailLevel = 0,
            VisibleNodeTypes = ["solution", "project", "assembly", "module", "namespace"],
            VisibleLinkTypes = ["contains", "declares", "project-reference", "references-assembly", "summary-depends-on", "summary-references"],
            NodeMetric = "importance", LinkMetric = "relationshipWeight", LayoutProfileId = "overview-space"
        },
        new()
        {
            Id = "architecture", Label = "Architecture", DetailLevel = 1,
            VisibleNodeTypes = ["project", "assembly", "module", "namespace", "file", "class", "interface", "record", "struct", "enum", "delegate"],
            VisibleLinkTypes = ["contains", "declares", "inherits", "implements", "summary-depends-on"],
            NodeMetric = "importance", LinkMetric = "relationshipWeight", LayoutProfileId = "architecture-space"
        },
        new()
        {
            Id = "member-detail", Label = "Member detail", DetailLevel = 2,
            VisibleNodeTypes = ["class", "interface", "record", "struct", "enum", "delegate", "method", "constructor", "property", "field", "event", "operator", "local-function", "type-parameter"],
            VisibleLinkTypes = ["contains", "declares", "calls", "constructs", "inherits", "implements", "overrides", "reads", "writes", "uses-type", "returns-type", "parameter-type", "summary-calls", "summary-depends-on"],
            NodeMetric = "importance", LinkMetric = "relationshipWeight", LayoutProfileId = "member-space"
        }
    ];

    public static IReadOnlyList<GraphLayoutProfile> LayoutProfiles() =>
    [
        CreateLayout("overview-space", 72, 42, [("assembly", "namespace", 32), ("namespace", "namespace", 24)]),
        CreateLayout("architecture-space", 56, 30, [("namespace", "file", 20), ("namespace", "class", 24), ("class", "method", 16)]),
        CreateLayout("member-space", 40, 22, [("class", "method", 14), ("class", "property", 14), ("class", "field", 14)])
    ];

    public static IReadOnlyList<GraphProjection> Projections() =>
    [
        new() { Id = "member-to-architecture", Label = "Member to architecture", FromProfile = "member-detail", ToProfile = "architecture", LinkTypeId = "summary-depends-on" },
        new() { Id = "member-to-overview", Label = "Member to overview", FromProfile = "member-detail", ToProfile = "overview", LinkTypeId = "summary-depends-on" },
        new() { Id = "calls-to-architecture", Label = "Calls to architecture", FromProfile = "member-detail", ToProfile = "architecture", LinkTypeId = "summary-calls" },
        new() { Id = "calls-to-overview", Label = "Calls to overview", FromProfile = "member-detail", ToProfile = "overview", LinkTypeId = "summary-calls" }
    ];

    public static IReadOnlyList<GraphContainmentRule> ContainmentRules()
    {
        var rules = new List<GraphContainmentRule>
        {
            Rule("solution-project", "solution", "project"),
            Rule("project-assembly", "project", "assembly"),
            Rule("assembly-module", "assembly", "module"),
            Rule("assembly-namespace", "assembly", "namespace"),
            Rule("module-file", "module", "file"),
            Rule("namespace-file", "namespace", "file")
        };

        foreach (var type in TypeNodeIds)
        {
            rules.Add(Rule($"namespace-{type}", "namespace", type));
            foreach (var member in MemberNodeIds)
                rules.Add(Rule($"{type}-{member}", type, member));
            foreach (var nestedType in TypeNodeIds)
                rules.Add(Rule($"{type}-{nestedType}", type, nestedType));
        }

        return rules;
    }

    public static GraphHierarchy Hierarchy() => new() { ContainmentLinkTypes = ["contains"], MaxParents = 2, Acyclic = true };

    private static readonly string[] TypeNodeIds = ["class", "interface", "record", "struct", "enum", "delegate"];
    private static readonly string[] MemberNodeIds = ["method", "constructor", "property", "field", "event", "operator", "local-function", "type-parameter"];

    private static GraphMetricDefinition Raw(string label, string unit, string description) =>
        new() { Label = label, Unit = unit, Description = description, ValueKind = "raw" };

    private static GraphContainmentRule Rule(string id, string parentTypeId, string childTypeId) =>
        new() { Id = id, LinkTypeId = "contains", ParentTypeId = parentTypeId, ChildTypeId = childTypeId, MaxParents = 1, Acyclic = true };

    private static GraphLayoutProfile CreateLayout(string id, double groupDistance, double defaultDistance, (string Parent, string Child, double Distance)[] distances) => new()
    {
        Id = id,
        Label = id,
        GroupDistance = groupDistance,
        DefaultDistance = defaultDistance,
        ContainmentDistances = distances.Select(distance => new GraphContainmentDistance { ParentTypeId = distance.Parent, ChildTypeId = distance.Child, Distance = distance.Distance }).ToList()
    };
}
