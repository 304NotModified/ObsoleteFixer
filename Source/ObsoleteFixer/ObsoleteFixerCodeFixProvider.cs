using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ObsoleteFixer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObsoleteFixerCodeFixProvider))]
    [Shared]
    public class ObsoleteFixerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ObsoleteFixerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(Microsoft.CodeAnalysis.CodeFixes.CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            if (!HasValidProperties(diagnostic))
            {
                return;
            }

            var success = Enum.TryParse(diagnostic.Properties[ObsoleteFixerAnalyzer.SyntaxKindKey], false, out SyntaxKind syntaxKind);
            if (!success)
            {
                return;
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the type declaration identified by the diagnostic.
            var syntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().FirstOrDefault(s => s.IsKind(syntaxKind));

            if (!IsSupported(syntax))
            {
                return;
            }

            var replaceWith = diagnostic.Properties[ObsoleteFixerAnalyzer.ReplaceWithKey];
            if (!bool.TryParse(diagnostic.Properties[ObsoleteFixerAnalyzer.IsObsoleteTypeKey], out var isObsoleteTypeKey))
            {
                return;
            }

            // Register a code action that will invoke the fix.
            var title = $"Replace method call with `{replaceWith}`";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    // todo change to createChangedSolution?
                    c => ReplaceMethodCallsAsync(context.Document, syntax, c, replaceWith, isObsoleteTypeKey),
                    title),
                diagnostic);
        }

        private static bool HasValidProperties(Diagnostic diagnostic)
        {
            return diagnostic.Properties.ContainsKey(ObsoleteFixerAnalyzer.ReplaceWithKey)
                   && diagnostic.Properties.ContainsKey(ObsoleteFixerAnalyzer.SyntaxKindKey)
                   && diagnostic.Properties.ContainsKey(ObsoleteFixerAnalyzer.IsObsoleteTypeKey);
        }

        private static bool IsSupported(SyntaxNode node)
        {
            // Currently only member access supported
            return
                node is MemberAccessExpressionSyntax ||
                node is ObjectCreationExpressionSyntax ||
                node is InvocationExpressionSyntax invocationSyntax && invocationSyntax.Expression is MemberAccessExpressionSyntax;
        }

        private static async Task<Document> ReplaceMethodCallsAsync(Document document, SyntaxNode node,
            CancellationToken cancellationToken, string replaceWith, bool isObsoleteTypeKey)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var info = semanticModel.GetSymbolInfo(node, cancellationToken);

            var symbol = info.Symbol;
            if (symbol == null)
            {
                return document;
            }

            var context = new CodeFixContext(document, cancellationToken, replaceWith, isObsoleteTypeKey, symbol);
            if (await TryReplaceObjectCreation(node, context))
            {
                return context.Document;
            }

            if (await TryReplaceMemberAccessCall(node, symbol, context))
            {
                return context.Document;
            }

            if (await TryReplaceProperty(node, context))
            {
                return context.Document;
            }

            return document;
        }

        private static async Task<bool> TryReplaceMemberAccessCall(SyntaxNode node, ISymbol symbol, CodeFixContext context)
        {
            if (node is InvocationExpressionSyntax invocationSyntax &&
                invocationSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax &&
                symbol is IMethodSymbol method)
            {
                var newNameSyntax = GetNameSyntax(context.ReplaceWith);
                var newMemberAccess = CreateMemberAccessExpressionSyntax(newNameSyntax, memberAccessExpressionSyntax, context.IsObsoleteTypeKey);

                if (newMemberAccess == null)
                {
                    return false;
                }

                var newArgumentList = GetArgumentSyntaxList(invocationSyntax, context.ReplaceWith, newNameSyntax, method);
                var newInvokeSyntax = invocationSyntax.WithExpression(newMemberAccess).WithArgumentList(SyntaxFactory.ArgumentList(newArgumentList));

                var newDocument = await CreateNewDocumentAsync(context.Document, invocationSyntax, newInvokeSyntax, context.CancellationToken);

                context.Document = newDocument;
                return true;
            }

            return false;
        }

        private static async Task<bool> TryReplaceObjectCreation(SyntaxNode node, CodeFixContext context)
        {
            if (node is ObjectCreationExpressionSyntax objectCreationExpressionSyntax)
            {
                var newTypeName = SyntaxFactory.ParseTypeName(context.ReplaceWith);

                if (newTypeName == null)
                {
                    return false;
                }

                var newObjectCreationExpressionSyntax = objectCreationExpressionSyntax.WithType(newTypeName);

                var newDocument = await CreateNewDocumentAsync(context.Document, objectCreationExpressionSyntax, newObjectCreationExpressionSyntax, context.CancellationToken);

                context.Document = newDocument;
                return true;
            }

            return false;
        }

        private static async Task<bool> TryReplaceProperty(SyntaxNode node, CodeFixContext context)
        {
            if (node is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                var newNameSyntax = GetNameSyntax(context.ReplaceWith);

                var newMemberAccess = CreateMemberAccessExpressionSyntax(newNameSyntax, memberAccessExpressionSyntax, context.IsObsoleteTypeKey);

                if (newMemberAccess == null)
                {
                    return false;
                }

                var newDocument = await CreateNewDocumentAsync(context.Document, memberAccessExpressionSyntax, newMemberAccess, context.CancellationToken);
                context.Document = newDocument;
                return true;
            }

            return false;
        }

        private static MemberAccessExpressionSyntax CreateMemberAccessExpressionSyntax(NameSyntax newNameSyntax, MemberAccessExpressionSyntax memberAccessExpressionSyntax, bool isObsoleteTypeKey)
        {
            if (newNameSyntax == null)
            {
                return null;
            }

            MemberAccessExpressionSyntax newMemberAccess = null;
            if (newNameSyntax is QualifiedNameSyntax qualifiedNameSyntax)
            {
                ExpressionSyntax expressionSyntax = qualifiedNameSyntax.Left;
                if (memberAccessExpressionSyntax.Expression is ObjectCreationExpressionSyntax objectCreationExpression)
                {
                    var typeSyntax = SyntaxFactory.ParseTypeName(qualifiedNameSyntax.Left.ToFullString());
                    if (typeSyntax != null)
                    {
                        var newObjectCreationExpression = objectCreationExpression.WithType(typeSyntax);
                        expressionSyntax = newObjectCreationExpression;
                    }
                }

                newMemberAccess = SyntaxFactory.MemberAccessExpression(memberAccessExpressionSyntax.Kind(), expressionSyntax, qualifiedNameSyntax.DotToken, qualifiedNameSyntax.Right);
            }
            else if (newNameSyntax is SimpleNameSyntax simpleNameSyntax)
            {
                if (isObsoleteTypeKey)
                {
                    newMemberAccess = memberAccessExpressionSyntax.WithExpression(simpleNameSyntax);
                }
                else
                {
                    newMemberAccess = memberAccessExpressionSyntax.WithName(simpleNameSyntax);
                }
            }

            newMemberAccess = newMemberAccess?.WithTriviaFrom(memberAccessExpressionSyntax);

            return newMemberAccess;
        }

        private static NameSyntax GetNameSyntax(string replaceWith)
        {
            var newNameSyntax = SyntaxFactory.ParseName(replaceWith, 0, false);
            return newNameSyntax;
        }

        private static async Task<Document> CreateNewDocumentAsync(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(oldNode, newNode);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        /// <summary>
        /// Try combine the arguments from the [Obsolete] and the current call
        /// </summary>
        /// <param name="invocationSyntax"></param>
        /// <param name="replaceWith"></param>
        /// <param name="newNameSyntax"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private static SeparatedSyntaxList<ArgumentSyntax> GetArgumentSyntaxList(InvocationExpressionSyntax invocationSyntax, string replaceWith,
            NameSyntax newNameSyntax, IMethodSymbol method)
        {
            var methodArguments = invocationSyntax.ArgumentList.Arguments;
            SeparatedSyntaxList<ArgumentSyntax> newArgumentList;
            if (replaceWith.Length > newNameSyntax.Span.End) // do we have args defined after the method name?
            {
                var argumentList = SyntaxFactory.ParseArgumentList(replaceWith, newNameSyntax.Span.End);

                var parameterValuesLookup = CreateParameterDict(method, methodArguments);

                var argumentValuesList = GetArgumentValuesList(argumentList, parameterValuesLookup);
                newArgumentList = ConvertToSeparatedSyntaxList(argumentValuesList);
            }
            else
            {
                // no arguments defined [Obsolete], so keep the args of the current call
                newArgumentList = methodArguments;
            }

            return newArgumentList;
        }

        /// <summary>
        /// Convert ExpressionSyntax list to SeparatedSyntaxList with ArgumentSyntax items
        /// </summary>
        private static SeparatedSyntaxList<ArgumentSyntax> ConvertToSeparatedSyntaxList(IEnumerable<ExpressionSyntax> argumentValues)
        {
            var argList = argumentValues.Select(SyntaxFactory.Argument).ToList();

            var argumentList = SyntaxFactory.SeparatedList(argList);
            return argumentList;
        }

        /// <summary>
        /// Get from argument (names) the needed argument values
        /// </summary>
        private static IEnumerable<ExpressionSyntax> GetArgumentValuesList(BaseArgumentListSyntax argumentList, IDictionary<string, ExpressionSyntax> argumentValues)
        {
            foreach (var arg in argumentList.Arguments)
            {
                var key = arg.ToFullString();
                if (argumentValues.TryGetValue(key, out var value))
                {
                    yield return value;
                }
                else
                {
                    // Not defined in old call, so use the value
                    var staticArg = SyntaxFactory.ParseArgumentList(key).Arguments.FirstOrDefault();

                    if (staticArg != null)
                    {
                        yield return staticArg.Expression;
                    }
                }
            }
        }

        /// <summary>
        /// Create dict with mapping between parameterKey and value (Expression)
        /// </summary>
        private static IDictionary<string, ExpressionSyntax> CreateParameterDict(IMethodSymbol method, SeparatedSyntaxList<ArgumentSyntax> oldArgs)
        {
            var index = 0;
            var dict = new Dictionary<string, ExpressionSyntax>(method.Parameters.Length);
            foreach (var methodParameter in method.Parameters)
            {
                var fullString = oldArgs[index].Expression;
                dict.Add(methodParameter.Name, fullString);
                index++;
            }

            return dict;
        }
    }
}