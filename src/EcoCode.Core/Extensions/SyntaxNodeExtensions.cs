﻿namespace EcoCode.Extensions;

/// <summary>Extensions methods for <see cref="SyntaxNode"/>.</summary>
public static class SyntaxNodeExtensions
{
    /// <summary>Returns whether the node is inside a loop.</summary>
    /// <param name="node">The node.</param>
    /// <returns>True if the node is inside a loop, false otherwise.</returns>
    public static bool IsInsideLoop(this SyntaxNode node)
    {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax)
                return true;
        }
        return false;
    }

    /// <summary>Returns the loop statements of a given node, if applicable.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The loop statements, empty if not applicable.</returns>
    public static SyntaxList<StatementSyntax> GetLoopStatements(this SyntaxNode node)
    {
        var contentSyntax = node switch
        {
            ForStatementSyntax syntax => syntax.Statement,
            ForEachStatementSyntax syntax => syntax.Statement,
            WhileStatementSyntax syntax => syntax.Statement,
            DoStatementSyntax syntax => syntax.Statement,
            _ => null,
        };

        return contentSyntax switch
        {
            BlockSyntax blockSyntax => blockSyntax.Statements,
            ExpressionStatementSyntax expressionSyntax => new(expressionSyntax), // Happens when the loop has only one statement
            _ => default,
        };
    }
}
