using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace EcoCode.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseStringLengthInsteadOfEmptyStringComparison : DiagnosticAnalyzer
    {
        private static readonly string DiagnosticId = "RCS1156";
        private static readonly LocalizableString Title = "Use string.Length instead of comparison with empty string";
        private static readonly LocalizableString MessageFormat = "Replace string comparison with empty string with 'Length' check";
        private static readonly LocalizableString Description = "Use 's?.Length == 0' instead of 's == \"\"' for performance and null safety.";
        private const string Category = "Style";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var binaryExpression = (BinaryExpressionSyntax)context.Node;

            // Look for the pattern 's == ""'
            if (binaryExpression.Left is IdentifierNameSyntax leftIdentifier &&
                binaryExpression.Right is LiteralExpressionSyntax rightLiteral && rightLiteral.Token.ValueText == "")
            {
                ReportDiagnostic(context, binaryExpression, leftIdentifier);
            }
            else if (binaryExpression.Right is IdentifierNameSyntax rightIdentifier &&
                     binaryExpression.Left is LiteralExpressionSyntax leftLiteral && leftLiteral.Token.ValueText == "")
            {
                ReportDiagnostic(context, binaryExpression, rightIdentifier);
            }
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, BinaryExpressionSyntax binaryExpression, IdentifierNameSyntax identifier)
        {
            // Suggest replacement with 's?.Length == 0'
            var diagnostic = Diagnostic.Create(Rule, binaryExpression.GetLocation(), identifier.Identifier.Text + "?.Length == 0");
            context.ReportDiagnostic(diagnostic);
        }
    }
}
