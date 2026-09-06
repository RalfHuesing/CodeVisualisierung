using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeVisualisierung.CSharp.Analysis;

internal static class SourceMetrics
{
    public static IReadOnlyList<int> GetNonEmptyLines(SyntaxNode syntax)
    {
        var text = syntax.SyntaxTree.GetText();
        var span = syntax.GetLocation().GetLineSpan();
        return Enumerable.Range(span.StartLinePosition.Line, span.EndLinePosition.Line - span.StartLinePosition.Line + 1)
            .Where(line => text.Lines[line].ToString().Trim().Length > 0)
            .Select(line => line + 1)
            .ToArray();
    }

    public static int GetCyclomaticComplexity(SyntaxNode syntax)
    {
        var complexity = 1;
        foreach (var node in syntax.DescendantNodes().Where(node => !IsNestedLocalFunction(node, syntax)))
        {
            complexity += node.Kind() switch
            {
                SyntaxKind.IfStatement or SyntaxKind.ForStatement or SyntaxKind.ForEachStatement
                    or SyntaxKind.WhileStatement or SyntaxKind.DoStatement or SyntaxKind.CaseSwitchLabel
                    or SyntaxKind.CatchClause or SyntaxKind.ConditionalExpression or SyntaxKind.CoalesceExpression
                    or SyntaxKind.SwitchExpressionArm or SyntaxKind.WhenClause
                    or SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression => 1,
                _ => 0
            };
        }

        return complexity;
    }

    private static bool IsNestedLocalFunction(SyntaxNode node, SyntaxNode root)
    {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
            if (parent is LocalFunctionStatementSyntax && parent != root)
                return true;
        return false;
    }
}
