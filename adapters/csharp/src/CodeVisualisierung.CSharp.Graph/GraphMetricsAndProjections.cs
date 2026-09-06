using CodeVisualisierung.CSharp.Contract;
namespace CodeVisualisierung.CSharp.Graph;

/// <summary>Reports bounded work counters from the complete metric pass.</summary>
public sealed record GraphAnalysisStats(long NormalizationValueScans);
/// <summary>Calculates complete-graph metrics and explicit summary projections.</summary>
public static class GraphMetricsAndProjections
{
    private const double PageRankDamping = 0.85;
    private const int PageRankIterations = 50;
    private const double PageRankTolerance = 1e-8;
    private static readonly string[] Levels = ["member", "type", "namespace", "assembly", "project"];
    private static readonly string[] MetricLevels = ["member", "type", "namespace"];
    private static readonly HashSet<string> RelationTypes =
    [
        "calls", "constructs", "inherits", "implements", "overrides", "reads", "writes",
        "uses-type", "returns-type", "parameter-type", "references-assembly"
    ];
    private static readonly HashSet<string> TypeNodeTypes = ["class", "interface", "record", "struct", "enum", "delegate"];
    private static readonly HashSet<string> MemberNodeTypes =
    ["method", "constructor", "property", "field", "event", "operator", "local-function", "type-parameter"];
    private static readonly HashSet<string> ContainerNodeTypes =
    ["solution", "project", "assembly", "module", "namespace", "file", "class", "interface", "record", "struct", "enum", "delegate"];
    public static GraphAnalysisStats Apply(GraphDocument graph)
    {
        var nodes = graph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var parents = CreateParentIndex(graph);
        var relationLinks = graph.Links.Where(link => RelationTypes.Contains(link.TypeId)).ToArray();

        var normalizationValueScans = ApplyLevelMetrics(nodes, parents, relationLinks);
        ApplyContainerMetrics(graph, nodes, parents);
        ApplyFootprints(nodes, ref normalizationValueScans);
        AddSummaryLinks(graph, nodes, parents, relationLinks);
        var sortedLinks = graph.Links.OrderBy(link => link.Id, StringComparer.Ordinal).ToArray();
        graph.Links.Clear();
        graph.Links.AddRange(sortedLinks);
        return new GraphAnalysisStats(normalizationValueScans);
    }
    private static long ApplyLevelMetrics(
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyDictionary<string, List<string>> parents,
        IReadOnlyCollection<GraphLink> relationLinks)
    {
        var normalizationValueScans = 0L;
        foreach (var level in MetricLevels)
        {
            var levelNodeIds = nodes.Values.Where(node => GetLevel(node) == level).Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            var edges = CreateProjectedEdges(level, nodes, parents, relationLinks);
            var counts = levelNodeIds.ToDictionary(nodeId => nodeId, _ => new int[4], StringComparer.Ordinal);
            foreach (var edge in edges.Values)
            {
                counts[edge.Source][1]++;
                counts[edge.Target][0]++;
                counts[edge.Source][3] += edge.RelationshipWeight;
                counts[edge.Target][2] += edge.RelationshipWeight;
            }
            foreach (var nodeId in levelNodeIds)
            {
                nodes[nodeId].Metrics["fanIn"] = counts[nodeId][0];
                nodes[nodeId].Metrics["fanOut"] = counts[nodeId][1];
                nodes[nodeId].Metrics["weightedFanIn"] = counts[nodeId][2];
                nodes[nodeId].Metrics["weightedFanOut"] = counts[nodeId][3];
            }

            var pageRanks = CalculatePageRank(levelNodeIds, edges.Values);
            var pageRankValues = pageRanks.Values.ToArray();
            var weightedInValues = levelNodeIds.Select(nodeId => nodes[nodeId].Metrics["weightedFanIn"]).ToArray();
            var normalizedWeightedInValues = weightedInValues.Select(LogOnePlus).ToArray();
            var pageRankParameters = CreateNormalizationParameters(pageRankValues, ref normalizationValueScans);
            var weightedFanInParameters = CreateNormalizationParameters(normalizedWeightedInValues, ref normalizationValueScans);
            foreach (var nodeId in levelNodeIds)
            {
                var pageRank = pageRanks[nodeId];
                nodes[nodeId].Metrics["pageRank"] = pageRank;
                var normalizedPageRank = RobustNormalize(pageRank, pageRankParameters);
                var normalizedFanIn = RobustNormalize(LogOnePlus(nodes[nodeId].Metrics["weightedFanIn"]), weightedFanInParameters);
                nodes[nodeId].Metrics["importance"] = Clamp(0.7 * normalizedPageRank + 0.3 * normalizedFanIn);
            }
        }

        return normalizationValueScans;
    }
    private static Dictionary<ProjectionKey, ProjectedEdge> CreateProjectedEdges(
        string level,
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyDictionary<string, List<string>> parents,
        IEnumerable<GraphLink> relationLinks)
    {
        var edges = new Dictionary<ProjectionKey, ProjectedEdge>();
        foreach (var link in relationLinks)
        {
            var source = GetAncestorAtLevel(link.Source, level, nodes, parents);
            var target = GetAncestorAtLevel(link.Target, level, nodes, parents);
            if (source is null || target is null)
                continue;

            var key = new ProjectionKey(source, target, link.TypeId);
            if (!edges.TryGetValue(key, out var edge))
            {
                edges[key] = new ProjectedEdge(source, target, GetOccurrences(link), GetRelationshipWeight(link));
                continue;
            }

            edges[key] = edge with
            {
                Occurrences = edge.Occurrences + GetOccurrences(link),
                RelationshipWeight = edge.RelationshipWeight + GetRelationshipWeight(link)
            };
        }

        return edges;
    }
    private static Dictionary<string, double> CalculatePageRank(
        IReadOnlyCollection<string> nodeIds,
        IEnumerable<ProjectedEdge> edges)
    {
        var orderedIds = nodeIds.Order(StringComparer.Ordinal).ToArray();
        var ranks = orderedIds.ToDictionary(id => id, _ => 1d / Math.Max(1, orderedIds.Length), StringComparer.Ordinal);
        if (orderedIds.Length == 0)
            return ranks;

        var outgoing = edges.GroupBy(edge => edge.Source, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        for (var iteration = 0; iteration < PageRankIterations; iteration++)
        {
            var next = orderedIds.ToDictionary(id => id, _ => (1 - PageRankDamping) / orderedIds.Length, StringComparer.Ordinal);
            var danglingMass = ranks.Where(pair => !outgoing.ContainsKey(pair.Key)).Sum(pair => pair.Value);
            foreach (var nodeId in orderedIds)
                next[nodeId] += PageRankDamping * danglingMass / orderedIds.Length;
            foreach (var group in outgoing)
            {
                var totalWeight = group.Value.Sum(edge => edge.RelationshipWeight);
                if (totalWeight <= 0)
                    continue;
                foreach (var edge in group.Value)
                    next[edge.Target] += PageRankDamping * ranks[group.Key] * edge.RelationshipWeight / totalWeight;
            }

            var change = orderedIds.Max(id => Math.Abs(next[id] - ranks[id]));
            ranks = next;
            if (change < PageRankTolerance)
                break;
        }

        return ranks;
    }
    private static void ApplyContainerMetrics(
        GraphDocument graph,
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyDictionary<string, List<string>> parents)
    {
        var fileByPath = graph.Nodes.Where(node => node.TypeId == "file")
            .Where(node => node.Attributes.TryGetValue("path", out var path) && path is string)
            .ToDictionary(node => $"{node.GroupId}\n{node.Attributes["path"]}", node => node.Id, StringComparer.OrdinalIgnoreCase);
        var facts = nodes.Values.ToDictionary(node => node.Id, node => CreateNodeFacts(node, fileByPath), StringComparer.Ordinal);
        foreach (var node in nodes.Values.OrderByDescending(node => GetContainmentDepth(node.Id, parents)))
        {
            foreach (var parentId in parents.GetValueOrDefault(node.Id, []))
                facts[parentId].Merge(facts[node.Id]);
        }

        foreach (var node in nodes.Values.Where(node => ContainerNodeTypes.Contains(node.TypeId)))
        {
            var fact = facts[node.Id];
            if (!node.Metrics.ContainsKey("loc") && fact.FileIds.Count > 0)
                node.Metrics["loc"] = fact.FileIds.Sum(fileId => nodes[fileId].Metrics.GetValueOrDefault("loc"));
            node.Metrics["fileCount"] = fact.FileIds.Count;
            node.Metrics["typeCount"] = fact.TypeIds.Count;
            node.Metrics["memberCount"] = fact.MemberIds.Count;
        }
    }

    private static NodeFacts CreateNodeFacts(GraphNode node, IReadOnlyDictionary<string, string> fileByPath)
    {
        var facts = new NodeFacts();
        if (node.TypeId == "file")
            facts.FileIds.Add(node.Id);
        if (TypeNodeTypes.Contains(node.TypeId))
            facts.TypeIds.Add(node.Id);
        if (MemberNodeTypes.Contains(node.TypeId))
            facts.MemberIds.Add(node.Id);

        if (node.Attributes.TryGetValue("declarationFiles", out var files) && files is IEnumerable<string> declarationFiles)
        {
            var projectId = node.Attributes.GetValueOrDefault("project") as string;
            foreach (var path in declarationFiles)
                if (fileByPath.TryGetValue($"{projectId}\n{path}", out var fileId))
                    facts.FileIds.Add(fileId);
        }

        return facts;
    }

    private static void ApplyFootprints(IReadOnlyDictionary<string, GraphNode> nodes, ref long normalizationValueScans)
    {
        var componentNames = new[] { "loc", "memberCount", "fileCount", "partialDeclarationCount" };
        var groups = nodes.Values.Where(node => GetMetricLevel(node) is not null).GroupBy(GetMetricLevel);
        foreach (var group in groups)
        {
            var normalizers = new Dictionary<string, NormalizationParameters>(StringComparer.Ordinal);
            foreach (var name in componentNames)
            {
                var values = group.Where(node => node.Metrics.ContainsKey(name))
                    .Select(node => LogOnePlus(node.Metrics[name])).ToArray();
                normalizers[name] = CreateNormalizationParameters(values, ref normalizationValueScans);
            }
            foreach (var node in group)
            {
                var values = componentNames.Where(name => node.Metrics.ContainsKey(name))
                    .Select(name => RobustNormalize(LogOnePlus(node.Metrics[name]), normalizers[name])).ToArray();
                if (values.Length > 0)
                    node.Metrics["footprint"] = Clamp(values.Average());
            }
        }
    }
    private static void AddSummaryLinks(
        GraphDocument graph,
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyDictionary<string, List<string>> parents,
        IEnumerable<GraphLink> relationLinks)
    {
        var summaries = new Dictionary<SummaryKey, SummaryEntry>();
        foreach (var link in relationLinks)
            AddSummaryEntries(summaries, link, nodes, parents);

        graph.Links.AddRange(summaries.Values.OrderBy(summary => summary.TypeId, StringComparer.Ordinal)
            .ThenBy(summary => summary.Source, StringComparer.Ordinal).ThenBy(summary => summary.Target, StringComparer.Ordinal)
            .Select(CreateSummaryLink));
    }

    private static void AddSummaryEntries(
        IDictionary<SummaryKey, SummaryEntry> summaries,
        GraphLink link,
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyDictionary<string, List<string>> parents)
    {
        if (link.TypeId == "references-assembly")
        {
            AddSummaryAtLevel(summaries, link, "assembly", nodes, parents);
            return;
        }

        var sourceLevel = GetLevel(nodes[link.Source]);
        var targetLevel = GetLevel(nodes[link.Target]);
        if (sourceLevel is null || targetLevel is null)
            return;
        var firstProjectionRank = Math.Min(GetLevelRank(sourceLevel), GetLevelRank(targetLevel)) + 1;
        foreach (var level in Levels.Where(level => GetLevelRank(level) >= firstProjectionRank))
            AddSummaryAtLevel(summaries, link, level, nodes, parents);
    }

    private static void AddSummaryAtLevel(
        IDictionary<SummaryKey, SummaryEntry> summaries,
        GraphLink link,
        string level,
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyDictionary<string, List<string>> parents)
    {
        var source = GetAncestorAtLevel(link.Source, level, nodes, parents);
        var target = GetAncestorAtLevel(link.Target, level, nodes, parents);
        if (source is null || target is null || source == target)
            return;
        var summaryType = link.TypeId switch
        {
            "calls" => "summary-calls",
            "references-assembly" => "summary-references",
            _ => "summary-depends-on"
        };
        var key = new SummaryKey(summaryType, source, target, level);
        var summary = summaries.TryGetValue(key, out var existing)
            ? existing
            : new SummaryEntry(source, target, level, summaryType, [], 0, 0);
        var detailLinkIds = new HashSet<string>(summary.DetailLinkIds, StringComparer.Ordinal) { link.Id };
        summaries[key] = summary with
        {
            DetailLinkIds = detailLinkIds,
            Occurrences = summary.Occurrences + GetOccurrences(link),
            RelationshipWeight = summary.RelationshipWeight + GetRelationshipWeight(link)
        };
    }

    private static GraphLink CreateSummaryLink(SummaryEntry summary) => new()
    {
        Id = $"link:{summary.TypeId}:{summary.Source}:{summary.Target}",
        Source = summary.Source,
        Target = summary.Target,
        TypeId = summary.TypeId,
        Summary = true,
        DerivedFrom = summary.DetailLinkIds.Order(StringComparer.Ordinal).ToList(),
        Weight = summary.RelationshipWeight,
        Metrics = new Dictionary<string, double>
        {
            ["occurrences"] = summary.Occurrences,
            ["relationshipWeight"] = summary.RelationshipWeight
        },
        Attributes = new Dictionary<string, object?>
        {
            ["aggregation"] = new Dictionary<string, object?>
            {
                ["origin"] = "detail-relations",
                ["sourceLevel"] = summary.Level,
                ["targetLevel"] = summary.Level,
                ["detailLinkCount"] = summary.DetailLinkIds.Count
            }
        }
    };

    private static Dictionary<string, List<string>> CreateParentIndex(GraphDocument graph) => graph.Links
        .Where(link => link.TypeId == "contains")
        .GroupBy(link => link.Target, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Select(link => link.Source).Order(StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

    private static string? GetAncestorAtLevel(
        string nodeId,
        string level,
        IReadOnlyDictionary<string, GraphNode> nodes,
        IReadOnlyDictionary<string, List<string>> parents)
    {
        var pending = new Queue<string>([nodeId]);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!visited.Add(current))
                continue;
            if (GetLevel(nodes[current]) == level)
                return current;
            foreach (var parentId in parents.GetValueOrDefault(current, []))
                pending.Enqueue(parentId);
        }

        return null;
    }

    private static int GetContainmentDepth(string nodeId, IReadOnlyDictionary<string, List<string>> parents)
    {
        var depth = 0;
        var current = nodeId;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (parents.TryGetValue(current, out var parentIds) && parentIds.Count > 0 && visited.Add(current))
        {
            depth++;
            current = parentIds[0];
        }

        return depth;
    }

    private static string? GetLevel(GraphNode node) => MemberNodeTypes.Contains(node.TypeId) ? "member"
        : TypeNodeTypes.Contains(node.TypeId) ? "type"
        : node.TypeId == "namespace" ? "namespace"
        : node.TypeId == "assembly" ? "assembly"
        : node.TypeId == "project" ? "project"
        : null;

    private static string? GetMetricLevel(GraphNode node) => GetLevel(node) ?? (ContainerNodeTypes.Contains(node.TypeId) ? "container" : null);

    private static int GetLevelRank(string level) => Array.IndexOf(Levels, level);

    private static int GetOccurrences(GraphLink link) => (int)link.Metrics.GetValueOrDefault("occurrences", 1);

    private static int GetRelationshipWeight(GraphLink link) => (int)link.Metrics.GetValueOrDefault("relationshipWeight", link.Weight ?? 0);

    private static NormalizationParameters CreateNormalizationParameters(
        IReadOnlyList<double> values,
        ref long normalizationValueScans)
    {
        normalizationValueScans += values.Count;
        if (values.Count == 0)
            return new NormalizationParameters(0, 0);
        return new NormalizationParameters(Percentile(values, 0.05), Percentile(values, 0.95));
    }

    private static double RobustNormalize(double value, NormalizationParameters parameters) => parameters.High <= parameters.Low
        ? 0.5
        : Clamp((value - parameters.Low) / (parameters.High - parameters.Low));

    private static double Percentile(IReadOnlyList<double> values, double fraction)
    {
        var position = (values.Count - 1) * fraction;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        var lowerValue = SelectKth(values.ToArray(), lower);
        if (lower == upper)
            return lowerValue;
        var upperValue = SelectKth(values.ToArray(), upper);
        return lowerValue + (upperValue - lowerValue) * (position - lower);
    }

    private static double SelectKth(double[] values, int index)
    {
        var left = 0;
        var right = values.Length - 1;
        while (left < right)
        {
            var pivotIndex = SelectPivotIndex(values, left, right);
            var (lower, upper) = Partition(values, left, right, pivotIndex);
            if (index < lower)
                right = lower - 1;
            else if (index > upper)
                left = upper + 1;
            else
                return values[index];
        }

        return values[left];
    }

    private static int SelectPivotIndex(double[] values, int left, int right)
    {
        if (right - left < 5)
        {
            SortSmallRange(values, left, right);
            return left + (right - left) / 2;
        }

        var medianEnd = left;
        for (var groupStart = left; groupStart <= right; groupStart += 5)
        {
            var groupEnd = Math.Min(groupStart + 4, right);
            SortSmallRange(values, groupStart, groupEnd);
            Swap(values, medianEnd++, groupStart + (groupEnd - groupStart) / 2);
        }

        return SelectPivotIndex(values, left, medianEnd - 1);
    }

    private static (int Lower, int Upper) Partition(double[] values, int left, int right, int pivotIndex)
    {
        var pivot = values[pivotIndex];
        var lower = left;
        var current = left;
        var upper = right;
        while (current <= upper)
        {
            if (values[current] < pivot)
                Swap(values, lower++, current++);
            else if (values[current] > pivot)
                Swap(values, current, upper--);
            else
                current++;
        }

        return (lower, upper);
    }

    private static void SortSmallRange(double[] values, int left, int right)
    {
        for (var index = left + 1; index <= right; index++)
        {
            var value = values[index];
            var insert = index - 1;
            while (insert >= left && values[insert] > value)
            {
                values[insert + 1] = values[insert--];
            }

            values[insert + 1] = value;
        }
    }

    private static void Swap(double[] values, int left, int right)
    {
        (values[left], values[right]) = (values[right], values[left]);
    }

    private static double Clamp(double value) => Math.Min(1, Math.Max(0, value));

    private static double LogOnePlus(double value) => Math.Log(value + 1);

    private sealed record NormalizationParameters(double Low, double High);

    private sealed class NodeFacts
    {
        public HashSet<string> FileIds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> TypeIds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> MemberIds { get; } = new(StringComparer.Ordinal);

        public void Merge(NodeFacts other)
        {
            FileIds.UnionWith(other.FileIds);
            TypeIds.UnionWith(other.TypeIds);
            MemberIds.UnionWith(other.MemberIds);
        }
    }

    private sealed record ProjectedEdge(string Source, string Target, int Occurrences, int RelationshipWeight);
    private sealed record ProjectionKey(string Source, string Target, string TypeId);
    private sealed record SummaryKey(string TypeId, string Source, string Target, string Level);

    private sealed record SummaryEntry(
        string Source,
        string Target,
        string Level,
        string TypeId,
        HashSet<string> DetailLinkIds,
        int Occurrences,
        int RelationshipWeight);
}
