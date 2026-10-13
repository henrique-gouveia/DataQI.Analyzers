using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

namespace DataQI.Analyzers.Completion.Support
{
    public class JunctionCompletionItemsBuilder : ICompletionItemsBuilder
    {
        public IEnumerable<CompletionItem> Build(string identifierSoFar, IReadOnlyList<IPropertySymbol> properties, string lastOperator)
        {
            foreach (var junction in new[] { "And", "Or" })
            {
                var displayText = identifierSoFar + junction;
                yield return CompletionItem.Create(
                    displayText: displayText,
                    sortText: $"0000_{displayText}",
                    tags: ImmutableArray.Create(WellKnownTags.Keyword));
            }
        }
    }
}
