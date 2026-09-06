using Microsoft.CodeAnalysis;

namespace CodeVisualisierung.CSharp.Analysis;

internal static class SemanticTestMethodDetection
{
    private static readonly string[] TestAttributeTypeNames =
    [
        "Xunit.FactAttribute",
        "Xunit.TheoryAttribute",
        "NUnit.Framework.TestAttribute",
        "NUnit.Framework.TestCaseAttribute",
        "NUnit.Framework.TestCaseSourceAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute"
    ];

    public static bool IsTestMethod(ISymbol symbol) => symbol is IMethodSymbol method
        && method.GetAttributes().Any(attribute => attribute.AttributeClass is { } attributeType
            && TestAttributeTypeNames.Contains(attributeType.ToDisplayString(), StringComparer.Ordinal));
}
