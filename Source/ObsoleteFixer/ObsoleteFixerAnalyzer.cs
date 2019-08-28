using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ObsoleteFixer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ObsoleteFixerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ObsoleteFixer";

        internal const string ReplaceWithKey = "replaceWith";
        internal const string SyntaxKindKey = "syntaxKind";
        internal const string IsObsoleteTypeKey = "isObsoleteType";
        private const string Category = "Naming";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression, SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var info = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken);
            var symbol = info.Symbol;

            if (symbol == null)
            {
                // we will get here if the code has a compile error
                return;
            }

            var obsoleteAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
            if (obsoleteAttribute == null)
            {
                return;
            }

            // obsoleteAttribute used?
            var isObsoleteType = false;
            var fullText = GetReplaceTextOfObsoleteAttribute(symbol.GetAttributes(), obsoleteAttribute);
            var syntaxKind = context.Node.Kind();
            if (fullText == null && syntaxKind == SyntaxKind.ObjectCreationExpression && symbol.ContainingType.IsType)
            {
                fullText = GetReplaceTextOfObsoleteAttribute(symbol.ContainingType.GetAttributes(), obsoleteAttribute);
                isObsoleteType = true;
            }

            if (fullText == null)
            {
                return;
            }

            var replaceWith = ObsoleteTextParser.FindReplaceWithValue(fullText);
            if (replaceWith == null)
            {
                return;
            }

            var properties = new Dictionary<string, string>
            {
                {ReplaceWithKey, replaceWith},
                {SyntaxKindKey, syntaxKind.ToString()},
                {IsObsoleteTypeKey, isObsoleteType.ToString()}
            }.ToImmutableDictionary();

            var location = context.Node.GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location, properties, symbol.Name, replaceWith);
            context.ReportDiagnostic(diagnostic);
        }

        private static string GetReplaceTextOfObsoleteAttribute(ImmutableArray<AttributeData> attributes, INamedTypeSymbol obsoleteAttribute)
        {
            var attributeData = attributes.FirstOrDefault(x => x.AttributeClass == obsoleteAttribute);
            if (attributeData == null || attributeData.ConstructorArguments.Length == 0)
            {
                return null;
            }

            var fullText = attributeData.ConstructorArguments[0].Value;
            return fullText as string;
        }
    }
}