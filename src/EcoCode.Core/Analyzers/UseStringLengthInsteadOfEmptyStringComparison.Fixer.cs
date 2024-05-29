using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace EcoCode.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseStringLengthInsteadOfEmptyStringComparisonFixer))]
    public sealed class UseStringLengthInsteadOfEmptyStringComparisonFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [UseStringLengthInsteadOfEmptyStringComparison.DiagnosticId];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Trouver le BinaryExpressionSyntax qui correspond au diagnostic.
                var binaryExpression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();

                // Enregistrer un code fix pour remplacer la comparaison avec une vérification de la longueur.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Use string.Length instead of comparison with empty string",
                        createChangedDocument: c => UseStringLengthAsync(context.Document, binaryExpression, c),
                        equivalenceKey: nameof(UseStringLengthInsteadOfEmptyStringComparisonFixer)),
                    diagnostic);
            }
        }

        private async Task<Document> UseStringLengthAsync(Document document, BinaryExpressionSyntax binaryExpression, CancellationToken cancellationToken)
        {
            // Créer une nouvelle expression en utilisant 'Length'.
            var identifier = binaryExpression.Left is IdentifierNameSyntax ? (IdentifierNameSyntax)binaryExpression.Left : (IdentifierNameSyntax)binaryExpression.Right;
            var newExpression = SyntaxFactory.BinaryExpression(
                binaryExpression.Kind(),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    identifier,
                    SyntaxFactory.IdentifierName("Length")),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));

            var newRoot = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).ReplaceNode(binaryExpression, newExpression);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
