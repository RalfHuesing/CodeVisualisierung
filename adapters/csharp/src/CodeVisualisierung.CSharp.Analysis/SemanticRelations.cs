using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeVisualisierung.CSharp.Analysis;

internal static class SemanticRelationCollector
{
    private const int CallRelationWeight = 3;
    private const int HierarchyRelationWeight = 2;
    private const int ReferenceRelationWeight = 1;

    public static IReadOnlyList<RelationCandidate> Collect(
        SyntaxNode root,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        var relations = new List<RelationCandidate>();
        var typeUseKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in root.DescendantNodesAndSelf())
        {
            if (node is TypeSyntax typeSyntax && IsRootTypeSyntax(typeSyntax))
                AddTypeUses(relations, typeSyntax, semanticModel, projectId, cancellationToken, typeUseKeys);
            if (node is not TypeSyntax || node is IdentifierNameSyntax)
                CollectExecutableNode(relations, node, semanticModel, projectId, cancellationToken);
        }

        return relations;
    }
    public static IReadOnlyList<RelationCandidate> CollectDeclarationRelations(
        SyntaxNode root,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        var relations = new List<RelationCandidate>();
        foreach (var node in root.DescendantNodesAndSelf())
            CollectDeclarationNode(relations, node, semanticModel, projectId, cancellationToken);

        return relations;
    }
    private static void CollectExecutableNode(
        ICollection<RelationCandidate> relations,
        SyntaxNode node,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        switch (node)
        {
            case InvocationExpressionSyntax invocation:
                AddInvocation(relations, invocation, semanticModel, projectId, cancellationToken);
                break;
            case ObjectCreationExpressionSyntax creation:
                AddConstruction(relations, creation, semanticModel, projectId, cancellationToken);
                break;
            case ConstructorInitializerSyntax initializer:
                AddCall(relations, initializer, semanticModel.GetSymbolInfo(initializer, cancellationToken).Symbol,
                    semanticModel, projectId, cancellationToken);
                break;
            case BinaryExpressionSyntax binary:
                AddOperatorCall(relations, binary, semanticModel, projectId, cancellationToken);
                break;
            case PrefixUnaryExpressionSyntax prefix:
                AddOperatorCall(relations, prefix, semanticModel, projectId, cancellationToken);
                break;
            case PostfixUnaryExpressionSyntax postfix:
                AddOperatorCall(relations, postfix, semanticModel, projectId, cancellationToken);
                break;
            case MemberAccessExpressionSyntax memberAccess:
                AddMemberAccess(relations, memberAccess, semanticModel, projectId, cancellationToken);
                break;
            case MemberBindingExpressionSyntax memberBinding:
                AddMemberAccess(relations, memberBinding, semanticModel, projectId, cancellationToken);
                break;
            case ElementAccessExpressionSyntax elementAccess:
                AddMemberAccess(relations, elementAccess, semanticModel, projectId, cancellationToken);
                break;
            case IdentifierNameSyntax identifier when IsStandaloneMemberName(identifier):
                AddMemberAccess(relations, identifier, semanticModel, projectId, cancellationToken);
                break;
        }
    }
    private static void CollectDeclarationNode(
        ICollection<RelationCandidate> relations,
        SyntaxNode node,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        switch (node)
        {
            case TypeDeclarationSyntax { ParameterList: not null } type:
                AddPrimaryConstructorParameters(relations, type, semanticModel, projectId, cancellationToken);
                break;
            case BaseTypeDeclarationSyntax declaration:
                AddBaseTypeRelations(relations, declaration, semanticModel, projectId, cancellationToken);
                break;
            case MethodDeclarationSyntax or ConstructorDeclarationSyntax or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax or LocalFunctionStatementSyntax:
                AddMethodSignatureRelations(relations, node, semanticModel, projectId, cancellationToken);
                break;
            case DelegateDeclarationSyntax @delegate:
                AddDelegateSignatureRelations(relations, @delegate, semanticModel, projectId, cancellationToken);
                break;
            case PropertyDeclarationSyntax property:
                AddPropertyTypeRelation(relations, property, semanticModel, projectId, cancellationToken);
                break;
            case IndexerDeclarationSyntax indexer:
                AddPropertyTypeRelation(relations, indexer, semanticModel, projectId, cancellationToken);
                break;
            case EventDeclarationSyntax @event:
                AddEventTypeRelation(relations, @event, semanticModel, projectId, cancellationToken);
                break;
            case VariableDeclaratorSyntax variable when IsFieldOrEventVariable(variable):
                AddFieldTypeRelation(relations, variable, semanticModel, projectId, cancellationToken);
                break;
        }
    }
    private static void AddInvocation(
        ICollection<RelationCandidate> relations,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        AddCall(relations, invocation, semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol,
            semanticModel, projectId, cancellationToken);
    }
    private static void AddConstruction(
        ICollection<RelationCandidate> relations,
        ObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        var constructor = semanticModel.GetSymbolInfo(creation, cancellationToken).Symbol as IMethodSymbol;
        var constructedType = constructor?.ContainingType ?? semanticModel.GetTypeInfo(creation.Type, cancellationToken).Type;
        AddRelation(relations, "constructs", FindSourceReference(creation, semanticModel, projectId, cancellationToken),
            CreateReference(constructedType, null), CallRelationWeight);
    }
    private static void AddOperatorCall(
        ICollection<RelationCandidate> relations,
        SyntaxNode expression,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        if (symbol is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion })
            AddCall(relations, expression, symbol, semanticModel, projectId, cancellationToken);
    }
    private static void AddCall(
        ICollection<RelationCandidate> relations,
        SyntaxNode syntax,
        ISymbol? target,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        AddRelation(relations, "calls", FindSourceReference(syntax, semanticModel, projectId, cancellationToken),
            CreateReference(target, null), CallRelationWeight);
    }

    private static void AddMemberAccess(
        ICollection<RelationCandidate> relations,
        SyntaxNode access,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        var target = semanticModel.GetSymbolInfo(access, cancellationToken).Symbol;
        if (target is not (IFieldSymbol or IPropertySymbol or IEventSymbol))
            return;

        var source = FindSourceReference(access, semanticModel, projectId, cancellationToken);
        var targetReference = CreateReference(target, null);
        switch (GetAccessKind(access))
        {
            case AccessKind.Read:
                AddRelation(relations, "reads", source, targetReference, ReferenceRelationWeight);
                break;
            case AccessKind.Write:
                AddRelation(relations, "writes", source, targetReference, ReferenceRelationWeight);
                break;
            case AccessKind.ReadAndWrite:
                AddRelation(relations, "reads", source, targetReference, ReferenceRelationWeight);
                AddRelation(relations, "writes", source, targetReference, ReferenceRelationWeight);
                break;
        }
    }

    private static void AddTypeUses(
        ICollection<RelationCandidate> relations,
        TypeSyntax typeSyntax,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken,
        ISet<string> typeUseKeys)
    {
        foreach (var type in GetRelatedTypes(semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type))
        {
            var key = $"{typeSyntax.Span.Start}:{typeSyntax.Span.Length}:{CSharpSymbolIdentity.GetCanonicalNodeSignature(type)}";
            if (!typeUseKeys.Add(key))
                continue;
            AddRelation(relations, "uses-type",
                FindSourceReference(typeSyntax, semanticModel, projectId, cancellationToken),
                CreateReference(type, null), ReferenceRelationWeight);
        }
    }

    private static void AddBaseTypeRelations(
        ICollection<RelationCandidate> relations,
        BaseTypeDeclarationSyntax declaration,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol sourceType
            || declaration.BaseList is null)
            return;

        var source = CreateReference(sourceType, projectId);
        foreach (var baseType in declaration.BaseList.Types)
        {
            var targetType = semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type as INamedTypeSymbol;
            var linkType = sourceType.TypeKind == TypeKind.Interface || IsBaseType(sourceType, targetType)
                ? "inherits"
                : "implements";
            AddRelation(relations, linkType, source, CreateReference(targetType, null), HierarchyRelationWeight);
        }
    }

    private static void AddMethodSignatureRelations(
        ICollection<RelationCandidate> relations,
        SyntaxNode declaration,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken) as IMethodSymbol;
        if (symbol is null)
            return;

        var source = CreateReference(symbol, projectId);
        if (!symbol.ReturnsVoid)
            AddTypeRelations(relations, "returns-type", source, symbol.ReturnType, projectId, ReferenceRelationWeight);
        foreach (var parameter in symbol.Parameters)
            AddTypeRelations(relations, "parameter-type", source, parameter.Type, projectId, ReferenceRelationWeight);
        AddOverrideRelation(relations, source, symbol.OverriddenMethod, projectId);
    }
    private static void AddDelegateSignatureRelations(
        ICollection<RelationCandidate> relations,
        DelegateDeclarationSyntax declaration,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type
            || type.DelegateInvokeMethod is not { } invoke)
            return;

        var source = CreateReference(type, projectId);
        if (!invoke.ReturnsVoid)
            AddTypeRelations(relations, "returns-type", source, invoke.ReturnType, projectId, ReferenceRelationWeight);
        foreach (var parameter in invoke.Parameters)
            AddTypeRelations(relations, "parameter-type", source, parameter.Type, projectId, ReferenceRelationWeight);
    }
    private static void AddPropertyTypeRelation(
        ICollection<RelationCandidate> relations,
        SyntaxNode declaration,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IPropertySymbol property)
        {
            var source = CreateReference(property, projectId);
            AddTypeRelations(relations, "uses-type", source, property.Type, projectId, ReferenceRelationWeight);
            foreach (var parameter in property.Parameters)
                AddTypeRelations(relations, "parameter-type", source, parameter.Type, projectId, ReferenceRelationWeight);
            AddOverrideRelation(relations, source, property.OverriddenProperty, projectId);
        }
    }

    private static void AddEventTypeRelation(
        ICollection<RelationCandidate> relations,
        SyntaxNode declaration,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IEventSymbol @event)
        {
            AddTypeRelations(relations, "uses-type", CreateReference(@event, projectId), @event.Type, projectId, ReferenceRelationWeight);
            AddOverrideRelation(relations, CreateReference(@event, projectId), @event.OverriddenEvent, projectId);
        }
    }

    private static void AddFieldTypeRelation(
        ICollection<RelationCandidate> relations,
        VariableDeclaratorSyntax declaration,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        switch (semanticModel.GetDeclaredSymbol(declaration, cancellationToken))
        {
            case IFieldSymbol field:
                AddTypeRelations(relations, "uses-type", CreateReference(field, projectId), field.Type, projectId, ReferenceRelationWeight);
                break;
            case IEventSymbol @event:
                var source = CreateReference(@event, projectId);
                AddTypeRelations(relations, "uses-type", source, @event.Type, projectId, ReferenceRelationWeight);
                AddOverrideRelation(relations, source, @event.OverriddenEvent, projectId);
                break;
        }
    }

    private static void AddPrimaryConstructorParameters(
        ICollection<RelationCandidate> relations,
        TypeDeclarationSyntax declaration,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
            return;

        var names = declaration.ParameterList!.Parameters.Select(parameter => parameter.Identifier.ValueText).ToArray();
        var constructor = type.InstanceConstructors.FirstOrDefault(candidate =>
            candidate.Parameters.Select(parameter => parameter.Name).SequenceEqual(names, StringComparer.Ordinal));
        if (constructor is null)
            return;

        var source = CreateReference(constructor, projectId);
        foreach (var parameter in constructor.Parameters)
            AddTypeRelations(relations, "parameter-type", source, parameter.Type, projectId, ReferenceRelationWeight);
    }

    private static void AddOverrideRelation(
        ICollection<RelationCandidate> relations,
        SymbolReference? source,
        ISymbol? overridden,
        string projectId)
    {
        if (overridden is not null)
            AddRelation(relations, "overrides", source, CreateReference(overridden, null), HierarchyRelationWeight);
    }

    private static void AddTypeRelations(
        ICollection<RelationCandidate> relations,
        string relationType,
        SymbolReference? source,
        ITypeSymbol? type,
        string projectId,
        int relationshipWeight)
    {
        foreach (var relatedType in GetRelatedTypes(type))
            AddRelation(relations, relationType, source, CreateReference(relatedType, null), relationshipWeight);
    }

    private static void AddRelation(
        ICollection<RelationCandidate> relations,
        string relationType,
        SymbolReference? source,
        SymbolReference? target,
        int relationshipWeight) => relations.Add(new RelationCandidate(relationType, source, target, relationshipWeight));

    private static SymbolReference? FindSourceReference(
        SyntaxNode syntax,
        SemanticModel semanticModel,
        string projectId,
        CancellationToken cancellationToken)
    {
        for (var symbol = semanticModel.GetEnclosingSymbol(syntax.SpanStart, cancellationToken);
             symbol is not null;
             symbol = symbol.ContainingSymbol)
        {
            var normalized = NormalizeSymbol(symbol);
            if (normalized is not null && !string.IsNullOrEmpty(CSharpSymbolIdentity.GetNodeTypeId(normalized)))
                return CreateReference(normalized, projectId);
        }

        return null;
    }

    private static SymbolReference? CreateReference(ISymbol? symbol, string? projectId)
    {
        var normalized = NormalizeSymbol(symbol);
        if (normalized is null)
            return null;

        var nodeTypeId = CSharpSymbolIdentity.GetNodeTypeId(normalized);
        var location = normalized.Locations
            .Where(candidate => candidate.IsInSource && candidate.SourceTree?.FilePath is not null)
            .OrderBy(candidate => candidate.SourceTree!.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.SourceSpan.Start)
            .FirstOrDefault();
        return new SymbolReference(
            nodeTypeId,
            CSharpSymbolIdentity.GetCanonicalNodeSignature(normalized),
            location?.SourceTree?.FilePath,
            projectId,
            CSharpSymbolIdentity.IsGeneratedSymbol(normalized),
            string.IsNullOrEmpty(nodeTypeId) || location is null,
            SemanticTestMethodDetection.IsTestMethod(normalized));
    }

    private static ISymbol? NormalizeSymbol(ISymbol? symbol) => symbol switch
    {
        IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise, AssociatedSymbol: not null } accessor
            => accessor.AssociatedSymbol,
        IMethodSymbol method when method.ReducedFrom is not null => method.ReducedFrom.OriginalDefinition,
        IMethodSymbol method => method.OriginalDefinition,
        INamedTypeSymbol type => type.OriginalDefinition,
        IPropertySymbol property => property.OriginalDefinition,
        IEventSymbol @event => @event.OriginalDefinition,
        IFieldSymbol field => field.OriginalDefinition,
        _ => symbol
    };

    private static IEnumerable<ITypeSymbol> GetRelatedTypes(ITypeSymbol? type)
    {
        if (type is null || type.SpecialType == SpecialType.System_Void)
            yield break;
        switch (type)
        {
            case IArrayTypeSymbol array:
                foreach (var related in GetRelatedTypes(array.ElementType))
                    yield return related;
                yield break;
            case IPointerTypeSymbol pointer:
                foreach (var related in GetRelatedTypes(pointer.PointedAtType))
                    yield return related;
                yield break;
            case INamedTypeSymbol named:
                yield return named.OriginalDefinition;
                foreach (var argument in named.TypeArguments)
                    foreach (var related in GetRelatedTypes(argument))
                        yield return related;
                yield break;
            default:
                yield return type;
                yield break;
        }
    }

    private static bool IsBaseType(INamedTypeSymbol source, INamedTypeSymbol? candidate) =>
        candidate is not null && source.BaseType is not null
        && SymbolEqualityComparer.Default.Equals(source.BaseType.OriginalDefinition, candidate.OriginalDefinition);

    private static bool IsFieldOrEventVariable(VariableDeclaratorSyntax variable) =>
        variable.Parent?.Parent is FieldDeclarationSyntax or EventFieldDeclarationSyntax;
    private static bool IsRootTypeSyntax(TypeSyntax typeSyntax) =>
        !typeSyntax.Ancestors().Any(ancestor => ancestor is TypeSyntax);
    private static bool IsStandaloneMemberName(IdentifierNameSyntax identifier) => identifier.Parent switch
    {
        MemberAccessExpressionSyntax member => !ReferenceEquals(member.Name, identifier),
        MemberBindingExpressionSyntax binding => !ReferenceEquals(binding.Name, identifier),
        _ => true
    };
    private static AccessKind GetAccessKind(SyntaxNode access)
    {
        if (IsAssignmentTarget(access))
            return GetAssignmentAccessKind((AssignmentExpressionSyntax)access.Parent!);
        if (IsIncrementAccess(access))
            return AccessKind.ReadAndWrite;
        if (access.Parent is ArgumentSyntax { RefKindKeyword.RawKind: not 0 } argument)
            return argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)
                ? AccessKind.Write
                : argument.RefKindKeyword.IsKind(SyntaxKind.InKeyword)
                    ? AccessKind.Read
                    : AccessKind.ReadAndWrite;
        return AccessKind.Read;
    }
    private static bool IsAssignmentTarget(SyntaxNode access) =>
        access.Parent is AssignmentExpressionSyntax assignment && ReferenceEquals(assignment.Left, access);

    private static AccessKind GetAssignmentAccessKind(AssignmentExpressionSyntax assignment) =>
        assignment.Kind() == SyntaxKind.SimpleAssignmentExpression ? AccessKind.Write : AccessKind.ReadAndWrite;
    private static bool IsIncrementAccess(SyntaxNode access) => access.Parent switch
    {
        PrefixUnaryExpressionSyntax prefix => IsIncrementOrDecrement(prefix.Kind()),
        PostfixUnaryExpressionSyntax postfix => IsIncrementOrDecrement(postfix.Kind()),
        _ => false
    };
    private static bool IsIncrementOrDecrement(SyntaxKind kind) =>
        kind is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression
            or SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression;
    private enum AccessKind
    {
        Read,
        Write,
        ReadAndWrite
    }
}
