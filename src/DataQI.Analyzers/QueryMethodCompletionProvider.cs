using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

using DataQI.Analyzers.Completion.Extensions;
using DataQI.Analyzers.Query;
using DataQI.Analyzers.Query.Support;
using DataQI.Analyzers.Repository;
using DataQI.Analyzers.Repository.Support;

namespace DataQI.Analyzers
{
    [ExportCompletionProvider(nameof(QueryMethodCompletionProvider), LanguageNames.CSharp)]
    [Shared]
    public class QueryMethodCompletionProvider : CompletionProvider
    {
        private const string PrefixText = "FindBy";
        private readonly IQueryTokenizer tokenizer;
        private readonly IRepositorySymbolResolver resolver;

        public QueryMethodCompletionProvider()
            : this(new QueryTokenizer(), new RepositorySymbolResolver())
        {
        }

        internal QueryMethodCompletionProvider(IQueryTokenizer tokenizer, IRepositorySymbolResolver resolver)
        {
            this.tokenizer = tokenizer;
            this.resolver = resolver;
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            if (trigger.Kind == CompletionTriggerKind.Invoke)
                return true;

            return trigger.Kind == CompletionTriggerKind.Insertion && char.IsLetter(trigger.Character);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
                var tokenPosition = context.Position > 0 ? context.Position - 1 : context.Position;
                var token = root.FindToken(tokenPosition);

                var identifierText = ExtractIdentifierText(token) ?? ExtractEmptyNameOnInvoke(token, context.Trigger);
                if (identifierText == null)
                    return;

                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
                if (!resolver.TryResolveEntityType(token, semanticModel, out var entityType))
                    return;

                if (!identifierText.StartsWith(PrefixText, StringComparison.Ordinal))
                {
                    AddPrefixItem(context, identifierText);
                    return;
                }

                var properties = resolver.GetPublicInstanceProperties(entityType);
                var propertyNames = properties.Select(p => p.Name).ToList();

                var tokens = tokenizer.Tokenize(identifierText, propertyNames);
                if (tokens.Count == 0)
                    return;

                var lastToken = tokens[tokens.Count - 1];
                var identifierSoFar = identifierText;
                var builderToken = lastToken;

                if (lastToken.Kind == QueryTokenKind.Incomplete)
                {
                    identifierSoFar = identifierText.Substring(0, lastToken.Start);
                    builderToken = tokens.Count > 1 ? tokens[tokens.Count - 2] : tokens[0];
                }

                var propertiesUsedSoFar = tokens
                    .Where(t => t.Kind == QueryTokenKind.Property)
                    .Select(t => properties.First(p => p.Name == t.Text))
                    .ToList();

                var builder = builderToken.GetCompletionItemsBuilder();
                if (builder == null)
                    return;

                var builderProperties = builderToken.Kind == QueryTokenKind.Prefix ||
                    builderToken.Kind == QueryTokenKind.Junction
                    ? properties
                    : propertiesUsedSoFar;

                foreach (var item in builder.Build(identifierSoFar, builderProperties, null))
                    context.AddItem(item);
            }
            catch
            {
                // swallow: never let a provider exception break the editor's completion list.
            }
        }

        private static string ExtractIdentifierText(SyntaxToken token)
        {
            if (token.Parent is VariableDeclaratorSyntax variableDeclarator)
                return variableDeclarator.Identifier.Text;

            if (token.Parent is MethodDeclarationSyntax methodDeclaration)
                return methodDeclaration.Identifier.Text;

            return null;
        }

        private static string ExtractEmptyNameOnInvoke(SyntaxToken token, CompletionTrigger trigger)
            => trigger.Kind == CompletionTriggerKind.Invoke && IsMemberNamePosition(token) ? string.Empty : null;

        // After "ReturnType " no identifier exists yet: Roslyn parses the member as an IncompleteMember whose
        // type is the last token before the caret.
        private static bool IsMemberNamePosition(SyntaxToken token)
        {
            var incompleteMember = token.Parent?.AncestorsAndSelf().OfType<IncompleteMemberSyntax>().FirstOrDefault();

            return incompleteMember != null
                && incompleteMember.Parent is InterfaceDeclarationSyntax
                && incompleteMember.Type != null
                && incompleteMember.Type.GetLastToken() == token;
        }

        private static void AddPrefixItem(CompletionContext context, string typedText)
        {
            if (!PrefixText.StartsWith(typedText, StringComparison.OrdinalIgnoreCase))
                return;

            context.AddItem(CompletionItem.Create(
                displayText: PrefixText,
                sortText: $"0000_{PrefixText}",
                tags: ImmutableArray.Create(WellKnownTags.Keyword)));
        }
    }
}
