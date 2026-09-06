using Microsoft.CodeAnalysis;

namespace CodeVisualisierung.CSharp.Fixtures;

/// <summary>
/// Fachliche Gruppen der semantischen Referenzfälle für spätere Roslyn-Analyse.
/// </summary>
public enum RoslynSemanticFixtureArea
{
    PartialTypes,
    OverloadsAndGenerics,
    InheritanceInterfacesAndOverrides,
    RecordsStructsEnumsAndDelegates,
    CallsConstructsReadsWritesAndTypeUsage
}

/// <summary>
/// Erwartete Quelldateien und Member eines semantischen Referenztyps.
/// </summary>
public sealed record RoslynSemanticFixtureCase(
    string Name,
    RoslynSemanticFixtureArea Area,
    string MetadataName,
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> RequiredMembers,
    TypeKind ExpectedTypeKind,
    bool ExpectedIsRecord);

/// <summary>
/// Kleine, absichtlich graphfreie Matrix für echte Symbolauflösungstests.
/// </summary>
public static class RoslynSemanticFixtureMatrix
{
    public static IReadOnlyList<RoslynSemanticFixtureCase> Declarations { get; } =
    [
        new(
            "Partial coordinator",
            RoslynSemanticFixtureArea.PartialTypes,
            "CodeVisualisierung.CSharp.Fixtures.Application.PartialCoordinator",
            ["PartialCoordinator.PartA.cs", "PartialCoordinator.PartB.cs"],
            ["Value", "Increment", "Describe"],
            TypeKind.Class,
            false),
        new(
            "Overloads and generic method",
            RoslynSemanticFixtureArea.OverloadsAndGenerics,
            "CodeVisualisierung.CSharp.Fixtures.Application.OverloadAndGeneric",
            ["SemanticSurface.cs"],
            ["Convert", "Echo"],
            TypeKind.Class,
            false),
        new(
            "Inheritance, interface and override",
            RoslynSemanticFixtureArea.InheritanceInterfacesAndOverrides,
            "CodeVisualisierung.CSharp.Fixtures.Application.DerivedProcessor",
            ["SemanticSurface.cs"],
            ["Process"],
            TypeKind.Class,
            false),
        new(
            "Records, structs, enums and delegates",
            RoslynSemanticFixtureArea.RecordsStructsEnumsAndDelegates,
            "CodeVisualisierung.CSharp.Fixtures.Application.ValueToken",
            ["SemanticSurface.cs"],
            ["Value"],
            TypeKind.Struct,
            true),
        new(
            "Enum declaration",
            RoslynSemanticFixtureArea.RecordsStructsEnumsAndDelegates,
            "CodeVisualisierung.CSharp.Fixtures.Application.ProcessingState",
            ["SemanticSurface.cs"],
            [],
            TypeKind.Enum,
            false),
        new(
            "Delegate declaration",
            RoslynSemanticFixtureArea.RecordsStructsEnumsAndDelegates,
            "CodeVisualisierung.CSharp.Fixtures.Application.StringTransformer",
            ["SemanticSurface.cs"],
            [],
            TypeKind.Delegate,
            false),
        new(
            "Semantic use site",
            RoslynSemanticFixtureArea.CallsConstructsReadsWritesAndTypeUsage,
            "CodeVisualisierung.CSharp.Fixtures.Application.SemanticUseSite",
            ["SemanticSurface.cs"],
            ["Execute"],
            TypeKind.Class,
            false)
    ];

    public static RoslynSemanticUseSite UseSite { get; } = new(
        "SemanticSurface.cs",
        ["Convert", "Process", "Describe", "Invoke"],
        ["DerivedProcessor", "ValueToken", "PartialCoordinator"],
        ["ValueToken.Value"],
        ["PartialCoordinator.Value"],
        ["DerivedProcessor", "ValueToken", "PartialCoordinator", "StringTransformer"]);
}

/// <summary>
/// Semantisch erwartete Referenzen an einer konkreten Aufrufstelle.
/// </summary>
public sealed record RoslynSemanticUseSite(
    string SourceFile,
    IReadOnlyList<string> InvocationNames,
    IReadOnlyList<string> ConstructedTypes,
    IReadOnlyList<string> ReadProperties,
    IReadOnlyList<string> WrittenProperties,
    IReadOnlyList<string> TypeUses);
