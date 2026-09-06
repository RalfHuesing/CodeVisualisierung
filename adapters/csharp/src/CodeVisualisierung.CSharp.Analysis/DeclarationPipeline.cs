using CodeVisualisierung.CSharp.Contract;
using CodeVisualisierung.CSharp.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeVisualisierung.CSharp.Analysis;

/// <summary>Creates stable C# node identities from Roslyn symbols.</summary>
public static class CSharpSymbolIdentity
{
    private static readonly SymbolDisplayFormat CanonicalFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    /// <summary>Returns the graph node type for a supported declaration symbol.</summary>
    public static string GetNodeTypeId(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol namedType => GetNamedTypeId(namedType),
        IMethodSymbol method when method.MethodKind == MethodKind.Constructor => "constructor",
        IMethodSymbol method when method.MethodKind == MethodKind.UserDefinedOperator => "operator",
        IMethodSymbol method when method.MethodKind == MethodKind.Conversion => "operator",
        IMethodSymbol method when method.MethodKind == MethodKind.LocalFunction => "local-function",
        IMethodSymbol => "method",
        IPropertySymbol => "property",
        IFieldSymbol => "field",
        IEventSymbol => "event",
        ITypeParameterSymbol => "type-parameter",
        _ => string.Empty
    };

    /// <summary>Builds the canonical project-scoped graph ID for a declaration.</summary>
    public static string CreateId(string nodeTypeId, string projectId, ISymbol symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeTypeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentNullException.ThrowIfNull(symbol);

        return $"{nodeTypeId}:{projectId}:{GetCanonicalNodeSignature(symbol)}";
    }

    /// <summary>Returns the fully qualified Roslyn display used as a stable signature.</summary>
    public static string GetCanonicalSignature(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } localFunction)
            return $"{GetContainingSignature(localFunction.ContainingSymbol)}.{localFunction.ToDisplayString(CanonicalFormat)}";
        if (symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise } accessor)
            return $"{GetCanonicalSignature(accessor.AssociatedSymbol!)}.accessor:{accessor.MethodKind}";
        return symbol.ToDisplayString(CanonicalFormat);
    }

    /// <summary>Returns the exact signature suffix used by the emitted node ID.</summary>
    internal static string GetCanonicalNodeSignature(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        if (symbol is not ITypeParameterSymbol typeParameter)
            return GetCanonicalSignature(symbol);

        var owner = typeParameter.ContainingSymbol is null
            ? string.Empty
            : GetCanonicalSignature(typeParameter.ContainingSymbol);
        return $"{owner}::{GetCanonicalSignature(typeParameter)}#{typeParameter.Ordinal}";
    }

    /// <summary>Returns a fully qualified name without a member parameter list.</summary>
    public static string GetQualifiedName(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        if (symbol is ITypeParameterSymbol typeParameter)
            return $"{GetQualifiedName(typeParameter.ContainingSymbol!)}.{typeParameter.Name}";
        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } localFunction)
            return $"{GetQualifiedName(localFunction.ContainingSymbol!)}.{localFunction.Name}";
        if (symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise } accessor)
            return $"{GetQualifiedName(accessor.AssociatedSymbol!)}.accessor:{accessor.MethodKind}";
        if (symbol is INamedTypeSymbol or INamespaceSymbol)
            return TrimGlobalPrefix(symbol.ToDisplayString(CanonicalFormat));

        var containingName = symbol.ContainingType is null
            ? TrimGlobalPrefix(symbol.ContainingNamespace?.ToDisplayString(CanonicalFormat) ?? string.Empty)
            : TrimGlobalPrefix(symbol.ContainingType.ToDisplayString(CanonicalFormat));
        return string.IsNullOrEmpty(containingName) ? symbol.Name : $"{containingName}.{symbol.Name}";
    }

    internal static bool IsExportedSymbol(ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared || string.IsNullOrEmpty(GetNodeTypeId(symbol)))
            return false;
        return symbol is not IMethodSymbol method || method.MethodKind is
            MethodKind.Constructor or MethodKind.Ordinary or MethodKind.UserDefinedOperator or
            MethodKind.Conversion or MethodKind.LocalFunction;
    }

    internal static bool IsGeneratedSymbol(ISymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.ContainingSymbol)
        {
            if (current.GetAttributes().Any(IsGeneratedCodeAttribute))
                return true;
        }

        return false;
    }

    private static string GetContainingSignature(ISymbol? symbol)
    {
        if (symbol is null)
            return string.Empty;
        return GetCanonicalSignature(symbol);
    }

    private static bool IsGeneratedCodeAttribute(AttributeData attribute) =>
        attribute.AttributeClass?.ToDisplayString() == "System.CodeDom.Compiler.GeneratedCodeAttribute";

    private static string GetNamedTypeId(INamedTypeSymbol symbol) => symbol.IsRecord
        ? "record"
        : symbol.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Interface => "interface",
            TypeKind.Struct => "struct",
            TypeKind.Enum => "enum",
            TypeKind.Delegate => "delegate",
            _ => string.Empty
        };

    private static string TrimGlobalPrefix(string value) => value.StartsWith("global::", StringComparison.Ordinal)
        ? value[8..]
        : value;
}

internal sealed record DeclarationFact(
    ISymbol Symbol,
    string NodeTypeId,
    string RelativePath,
    SourcePosition Position);

/// <summary>One-based source span with a solution-relative path.</summary>
public sealed record SourcePosition(string Path, int Line, int Column, int EndLine, int EndColumn);

internal sealed class DeclarationAccumulator
{
    private readonly Dictionary<string, DeclarationEntry> entries = new(StringComparer.Ordinal);

    public void Add(DeclarationFact fact)
    {
        var id = CSharpSymbolIdentity.CreateId(fact.NodeTypeId, ProjectId, fact.Symbol);
        if (!entries.TryGetValue(id, out var entry))
        {
            entry = new DeclarationEntry(id, fact.Symbol, fact.NodeTypeId);
            entries.Add(id, entry);
        }

        entry.Positions.Add(fact.Position);
    }

    public string ProjectId { get; }

    internal IEnumerable<(ISymbol Symbol, string Id, IReadOnlyList<SourcePosition> Positions)> ExportedEntries =>
        entries.Values.Select(entry => (entry.Symbol, entry.Id, (IReadOnlyList<SourcePosition>)entry.Positions));

    public DeclarationAccumulator(string projectId)
    {
        ProjectId = projectId;
    }

    public void Emit(GraphBuilder builder)
    {
        foreach (var entry in entries.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            var node = CreateNode(entry);
            builder.AddNode(node);
            AddContainment(entry, builder);
        }
    }

    private GraphNode CreateNode(DeclarationEntry entry)
    {
        var orderedPositions = entry.Positions
            .OrderBy(position => position.Path, StringComparer.Ordinal)
            .ThenBy(position => position.Line)
            .ThenBy(position => position.Column);
        var positions = orderedPositions.Distinct().ToArray();
        var attributes = new Dictionary<string, object?>
        {
            ["qualifiedName"] = CSharpSymbolIdentity.GetQualifiedName(entry.Symbol),
            ["signature"] = CSharpSymbolIdentity.GetCanonicalSignature(entry.Symbol),
            ["accessibility"] = GetAccessibility(entry.Symbol.DeclaredAccessibility),
            ["project"] = ProjectId,
            ["source"] = positions[0],
            ["sourcePositions"] = positions,
            ["declarationFiles"] = positions.Select(position => position.Path).Distinct(StringComparer.Ordinal).ToArray()
        };

        if (entry.Symbol is INamedTypeSymbol)
            attributes["partialDeclarationCount"] = positions.Length;

        var parentId = GetParentId(entry.Symbol);
        if (parentId is not null)
            attributes["containerId"] = parentId;

        return new GraphNode
        {
            Id = entry.Id,
            TypeId = entry.NodeTypeId,
            Label = GetLabel(entry.Symbol, entry.NodeTypeId),
            GroupId = parentId ?? ProjectId,
            Attributes = attributes
        };
    }

    private void AddContainment(DeclarationEntry entry, GraphBuilder builder)
    {
        var parentId = GetParentId(entry.Symbol);
        if (parentId is not null)
        {
            builder.AddLink("contains", parentId, entry.Id);
            builder.AddDeclarationLink(parentId, entry.Id);
        }

        foreach (var position in entry.Positions.Distinct())
            builder.AddDeclarationLink($"file:{ProjectId}:{position.Path}", entry.Id);
    }

    private string? GetParentId(ISymbol symbol)
    {
        var candidate = GetCandidateParentId(symbol);
        if (candidate is null || candidate.StartsWith("namespace:", StringComparison.Ordinal))
            return candidate;
        return entries.ContainsKey(candidate) ? candidate : null;
    }

    private string? GetCandidateParentId(ISymbol symbol)
    {
        if (symbol is ITypeParameterSymbol typeParameter)
            return GetSupportedParentId(typeParameter.ContainingSymbol);

        return symbol switch
        {
            INamedTypeSymbol namedType when namedType.ContainingType is not null => GetSupportedParentId(namedType.ContainingType),
            INamedTypeSymbol namedType => GetNamespaceId(namedType.ContainingNamespace),
            IMethodSymbol method when method.MethodKind == MethodKind.LocalFunction => GetSupportedParentId(method.ContainingSymbol),
            IMethodSymbol method => GetSupportedParentId(method.ContainingType),
            IPropertySymbol property => GetSupportedParentId(property.ContainingType),
            IFieldSymbol field => GetSupportedParentId(field.ContainingType),
            IEventSymbol @event => GetSupportedParentId(@event.ContainingType),
            _ => null
        };
    }

    private string? GetSupportedParentId(ISymbol? symbol)
    {
        if (symbol is null || !CSharpSymbolIdentity.IsExportedSymbol(symbol))
            return null;
        var nodeTypeId = CSharpSymbolIdentity.GetNodeTypeId(symbol);
        return string.IsNullOrEmpty(nodeTypeId) ? null : CSharpSymbolIdentity.CreateId(nodeTypeId, ProjectId, symbol);
    }

    private string GetNamespaceId(INamespaceSymbol? symbol)
    {
        var namespaceName = symbol is null ? "global" : GetNamespaceName(symbol);
        return $"namespace:{ProjectId}:{namespaceName}";
    }

    private static string GetNamespaceName(INamespaceSymbol symbol)
    {
        var name = symbol.ToDisplayString(CSharpSymbolIdentityFormat);
        if (name.StartsWith("global::", StringComparison.Ordinal))
            name = name[8..];
        return string.IsNullOrEmpty(name) ? "global" : name;
    }

    private static string GetLabel(ISymbol symbol, string nodeTypeId)
    {
        if (nodeTypeId == "operator")
            return $"operator {symbol.Name}";
        if (nodeTypeId == "constructor" && symbol.ContainingType is not null)
            return symbol.ContainingType.Name;
        return symbol.Name;
    }

    private static string GetAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        Accessibility.NotApplicable => "not-applicable",
        _ => accessibility.ToString().ToLowerInvariant()
    };

    private static readonly SymbolDisplayFormat CSharpSymbolIdentityFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    private sealed class DeclarationEntry(string id, ISymbol symbol, string nodeTypeId)
    {
        public string Id { get; } = id;
        public ISymbol Symbol { get; } = symbol;
        public string NodeTypeId { get; } = nodeTypeId;
        public List<SourcePosition> Positions { get; } = [];
    }
}

internal static class DeclarationCollector
{
    public static IReadOnlyList<DeclarationFact> Collect(
        SyntaxNode root,
        SemanticModel semanticModel,
        string rootPath,
        string projectId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(semanticModel);
        var relativePath = RelativePath(rootPath, root.SyntaxTree.FilePath);
        var facts = new List<DeclarationFact>();
        foreach (var syntax in GetDeclarationSyntax(root))
        {
            var symbol = semanticModel.GetDeclaredSymbol(syntax, cancellationToken);
            if (symbol is null || !IsSupported(symbol) || symbol.IsImplicitlyDeclared || CSharpSymbolIdentity.IsGeneratedSymbol(symbol))
                continue;

            facts.Add(CreateFact(symbol, syntax, relativePath));
        }

        AddPrimaryConstructorFacts(root, semanticModel, relativePath, facts, cancellationToken);

        return facts;
    }

    private static void AddPrimaryConstructorFacts(
        SyntaxNode root,
        SemanticModel semanticModel,
        string relativePath,
        ICollection<DeclarationFact> facts,
        CancellationToken cancellationToken)
    {
        foreach (var typeSyntax in root.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
        {
            if (typeSyntax.ParameterList is null)
                continue;
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeSyntax, cancellationToken);
            if (typeSymbol is not INamedTypeSymbol namedType)
                continue;
            var parameterNames = typeSyntax.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText).ToArray();
            var constructor = namedType.InstanceConstructors.FirstOrDefault(candidate =>
                candidate.Parameters.Select(parameter => parameter.Name).SequenceEqual(parameterNames, StringComparer.Ordinal)
                && !CSharpSymbolIdentity.IsGeneratedSymbol(candidate));
            if (constructor is not null)
                facts.Add(CreateFact(constructor, typeSyntax.ParameterList, relativePath));
        }
    }

    private static DeclarationFact CreateFact(ISymbol symbol, SyntaxNode syntax, string relativePath)
    {
        var position = syntax.GetLocation().GetLineSpan();
        return new DeclarationFact(
            symbol,
            CSharpSymbolIdentity.GetNodeTypeId(symbol),
            relativePath,
            new SourcePosition(
                relativePath,
                position.StartLinePosition.Line + 1,
                position.StartLinePosition.Character + 1,
                position.EndLinePosition.Line + 1,
                position.EndLinePosition.Character + 1));
    }

    private static readonly Type[] DeclarationSyntaxTypes =
    [
        typeof(BaseTypeDeclarationSyntax),
        typeof(DelegateDeclarationSyntax),
        typeof(MethodDeclarationSyntax),
        typeof(ConstructorDeclarationSyntax),
        typeof(PropertyDeclarationSyntax),
        typeof(IndexerDeclarationSyntax),
        typeof(EventDeclarationSyntax),
        typeof(OperatorDeclarationSyntax),
        typeof(ConversionOperatorDeclarationSyntax),
        typeof(LocalFunctionStatementSyntax),
        typeof(TypeParameterSyntax),
        typeof(EnumMemberDeclarationSyntax)
    ];

    private static IEnumerable<SyntaxNode> GetDeclarationSyntax(SyntaxNode root)
    {
        return root.DescendantNodesAndSelf().Where(IsDeclarationSyntax);
    }

    private static bool IsDeclarationSyntax(SyntaxNode node)
    {
        if (node is VariableDeclaratorSyntax variable)
            return IsFieldVariable(variable);

        return DeclarationSyntaxTypes.Any(type => type.IsInstanceOfType(node));
    }

    private static bool IsFieldVariable(VariableDeclaratorSyntax variable) =>
        variable.Parent?.Parent is FieldDeclarationSyntax or EventFieldDeclarationSyntax;

    private static bool IsSupported(ISymbol symbol) => !string.IsNullOrEmpty(CSharpSymbolIdentity.GetNodeTypeId(symbol))
        && symbol.Locations.Any(location => location.IsInSource);

    private static string RelativePath(string rootPath, string path) => Path.GetRelativePath(rootPath, path)
        .Replace(Path.DirectorySeparatorChar, '/')
        .Replace(Path.AltDirectorySeparatorChar, '/');
}
