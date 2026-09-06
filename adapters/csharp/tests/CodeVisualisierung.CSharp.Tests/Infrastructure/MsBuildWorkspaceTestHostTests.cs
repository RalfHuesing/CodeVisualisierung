#nullable enable

using System.Linq;
using CodeVisualisierung.CSharp.Fixtures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CodeVisualisierung.CSharp.Tests.Infrastructure;

[Trait("Category", "Integration")]
public sealed class MsBuildWorkspaceTestHostTests
{
    private readonly MsBuildWorkspaceTestHost host;

    public MsBuildWorkspaceTestHostTests(MsBuildWorkspaceTestHost host)
    {
        this.host = host;
    }

    [Fact]
    public void Loads_physical_solution_and_keeps_workspace_diagnostics_readable()
    {
        Assert.True(File.Exists(host.SolutionPath));
        Assert.Equal(3, host.Solution.Projects.Count());
        Assert.Contains(host.Solution.Projects, project => project.Name == "CSharpReferenceMini.Contracts");
        Assert.Contains(host.Solution.Projects, project => project.Name == "CSharpReferenceMini.Application");
        Assert.Empty(host.WorkspaceFailures);
    }

    [Fact]
    public async Task Resolves_project_reference_document_and_interface_invocation()
    {
        var contracts = host.Solution.Projects.Single(
            project => project.Name == "CSharpReferenceMini.Contracts");
        var application = host.Solution.Projects.Single(
            project => project.Name == "CSharpReferenceMini.Application");
        var service = application.Documents.Single(document => document.Name == "GreetingService.cs");

        Assert.Contains(application.ProjectReferences, reference => reference.ProjectId == contracts.Id);
        Assert.EndsWith(
            Path.Combine("src", "CSharpReferenceMini.Application", "GreetingService.cs"),
            service.FilePath,
            StringComparison.OrdinalIgnoreCase);

        var model = await service.GetSemanticModelAsync();
        var root = await service.GetSyntaxRootAsync();
        var invocation = root!.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single();
        var symbol = model!.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        Assert.NotNull(symbol);
        Assert.Equal("Greet", symbol!.Name);
        Assert.Equal("IGreeter", symbol.ContainingType.Name);
        Assert.Equal(
            "CodeVisualisierung.CSharp.Fixtures.Contracts",
            symbol.ContainingNamespace.ToDisplayString());
    }

    [Fact]
    public async Task Resolves_semantic_fixture_matrix_and_usage_symbols()
    {
        var application = host.Solution.Projects.Single(
            project => project.Name == "CSharpReferenceMini.Application");
        var compilation = await application.GetCompilationAsync();

        Assert.NotNull(compilation);
        Assert.DoesNotContain(
            compilation!.GetDiagnostics(),
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        AssertDeclarationMatrix(application, compilation);
        await AssertUsageMatrix(application);
        AssertInheritanceInterfaceAndOverride(compilation);
    }

    private static void AssertDeclarationMatrix(Project application, Compilation compilation)
    {
        var documentNames = application.Documents.Select(document => document.Name).ToHashSet();
        foreach (var fixtureCase in RoslynSemanticFixtureMatrix.Declarations)
        {
            Assert.All(fixtureCase.SourceFiles, sourceFile => Assert.Contains(sourceFile, documentNames));

            var type = compilation.GetTypeByMetadataName(fixtureCase.MetadataName);
            Assert.True(type is not null, $"Typ fehlt: {fixtureCase.MetadataName}");
            Assert.Equal(fixtureCase.ExpectedTypeKind, type!.TypeKind);
            Assert.Equal(fixtureCase.ExpectedIsRecord, type.IsRecord);
            Assert.All(fixtureCase.RequiredMembers, memberName =>
                Assert.Contains(type.GetMembers(memberName), member => member.Name == memberName));

            if (fixtureCase.Area == RoslynSemanticFixtureArea.PartialTypes)
            {
                Assert.Equal(2, type.DeclaringSyntaxReferences.Length);
            }
        }

        var overloads = compilation.GetTypeByMetadataName(
            "CodeVisualisierung.CSharp.Fixtures.Application.OverloadAndGeneric")!;
        var convertMethods = overloads.GetMembers("Convert").OfType<IMethodSymbol>().ToArray();
        Assert.Equal(2, convertMethods.Length);
        Assert.Contains(convertMethods, method =>
            method.Arity == 0 && method.Parameters.Single().Type.SpecialType == SpecialType.System_Int32);
        Assert.Contains(convertMethods, method =>
            method.Arity == 0 && method.Parameters.Single().Type.SpecialType == SpecialType.System_String);

        var echoMethods = overloads.GetMembers("Echo").OfType<IMethodSymbol>().ToArray();
        Assert.Single(echoMethods);
        Assert.Equal(1, echoMethods[0].Arity);
    }

    private static async Task AssertUsageMatrix(Project application)
    {
        var useSite = application.Documents.Single(
            document => document.Name == RoslynSemanticFixtureMatrix.UseSite.SourceFile);
        var model = await useSite.GetSemanticModelAsync();
        var root = await useSite.GetSyntaxRootAsync();

        AssertUsageInvocations(model!, root!);
        AssertUsageConstructions(model!, root!);
        AssertUsagePropertyAccesses(model!, root!);
        AssertUsageTypeUses(model!, root!);
    }

    private static void AssertUsageInvocations(SemanticModel model, SyntaxNode root)
    {
        var invocationNames = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => model.GetSymbolInfo(invocation).Symbol?.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(RoslynSemanticFixtureMatrix.UseSite.InvocationNames, name =>
            Assert.Contains(name, invocationNames));
    }

    private static void AssertUsageConstructions(SemanticModel model, SyntaxNode root)
    {
        var constructionSymbols = root.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Select(creation => model.GetSymbolInfo(creation).Symbol?.ContainingType.Name)
            .OfType<string>();
        var constructedTypes = constructionSymbols
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(RoslynSemanticFixtureMatrix.UseSite.ConstructedTypes, typeName =>
            Assert.Contains(typeName, constructedTypes));
    }

    private static void AssertUsagePropertyAccesses(SemanticModel model, SyntaxNode root)
    {
        var memberAccesses = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToArray();
        var readPropertySymbols = memberAccesses
            .Where(access => access.Parent is not AssignmentExpressionSyntax assignment ||
                             !ReferenceEquals(assignment.Left, access))
            .Select(access => model.GetSymbolInfo(access).Symbol)
            .OfType<IPropertySymbol>()
            .Select(property => $"{property.ContainingType.Name}.{property.Name}");
        var readProperties = readPropertySymbols
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(RoslynSemanticFixtureMatrix.UseSite.ReadProperties, property =>
            Assert.Contains(property, readProperties));

        var writtenPropertySymbols = root.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Select(assignment => model.GetSymbolInfo(assignment.Left).Symbol)
            .OfType<IPropertySymbol>();
        var writtenProperties = writtenPropertySymbols
            .Select(property => $"{property.ContainingType.Name}.{property.Name}")
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(RoslynSemanticFixtureMatrix.UseSite.WrittenProperties, property =>
            Assert.Contains(property, writtenProperties));
    }

    private static void AssertUsageTypeUses(SemanticModel model, SyntaxNode root)
    {
        var typeUses = root.DescendantNodes()
            .OfType<VariableDeclarationSyntax>()
            .Select(declaration => model.GetTypeInfo(declaration.Type).Type?.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(RoslynSemanticFixtureMatrix.UseSite.TypeUses, typeName =>
            Assert.Contains(typeName, typeUses));
    }

    private static void AssertInheritanceInterfaceAndOverride(Compilation compilation)
    {
        var derived = compilation.GetTypeByMetadataName(
            "CodeVisualisierung.CSharp.Fixtures.Application.DerivedProcessor");
        var processorInterface = compilation.GetTypeByMetadataName(
            "CodeVisualisierung.CSharp.Fixtures.Application.IProcessor");
        var process = derived!.GetMembers("Process").OfType<IMethodSymbol>().Single();
        var interfaceProcess = processorInterface!.GetMembers("Process").OfType<IMethodSymbol>().Single();

        Assert.Equal("BaseProcessor", derived.BaseType!.Name);
        Assert.Contains(derived.AllInterfaces, item => item.Name == "IProcessor");
        Assert.NotNull(process.OverriddenMethod);
        Assert.Equal(process, derived.FindImplementationForInterfaceMember(interfaceProcess));
    }
}
