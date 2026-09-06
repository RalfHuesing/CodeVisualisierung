using Microsoft.CodeAnalysis;

namespace CodeVisualisierung.CSharp.Analysis;

internal sealed record SymbolReference(
    string NodeTypeId,
    string Signature,
    string? SourcePath,
    string? ProjectId,
    bool IsGenerated,
    bool IsExternal,
    bool IsTestMethod);

internal sealed record RelationCandidate(
    string TypeId,
    SymbolReference? Source,
    SymbolReference? Target,
    int RelationshipWeight);

internal sealed class ExportedNodeIndex
{
    private readonly Dictionary<string, string> ids = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> projectsBySourcePath = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string projectId, ISymbol symbol, string nodeId)
    {
        var nodeTypeId = CSharpSymbolIdentity.GetNodeTypeId(symbol);
        var signature = CSharpSymbolIdentity.GetCanonicalNodeSignature(symbol);
        ids[Key(projectId, nodeTypeId, signature)] = nodeId;
        foreach (var location in symbol.Locations.Where(location => location.IsInSource && location.SourceTree?.FilePath is not null))
            projectsBySourcePath[Path.GetFullPath(location.SourceTree!.FilePath)] = projectId;
    }

    public bool TryResolve(SymbolReference reference, out string nodeId, out string projectId)
    {
        var resolvedProjectId = reference.ProjectId ?? ResolveProject(reference.SourcePath);
        if (string.IsNullOrEmpty(resolvedProjectId))
        {
            nodeId = string.Empty;
            projectId = string.Empty;
            return false;
        }

        projectId = resolvedProjectId;
        if (ids.TryGetValue(Key(resolvedProjectId, reference.NodeTypeId, reference.Signature), out var resolvedId))
        {
            nodeId = resolvedId;
            return true;
        }

        nodeId = string.Empty;
        return false;
    }

    private string? ResolveProject(string? sourcePath) => sourcePath is null
        ? null
        : projectsBySourcePath.TryGetValue(Path.GetFullPath(sourcePath), out var projectId) ? projectId : null;

    private static string Key(string projectId, string nodeTypeId, string signature) =>
        $"{projectId}\n{nodeTypeId}\n{signature}";
}
