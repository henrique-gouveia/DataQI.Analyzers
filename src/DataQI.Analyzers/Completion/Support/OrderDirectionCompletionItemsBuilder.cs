using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

namespace DataQI.Analyzers.Completion.Support
{
    public class OrderDirectionCompletionItemsBuilder : ICompletionItemsBuilder
    {
        public IEnumerable<CompletionItem> Build(string identifierSoFar, IReadOnlyList<IPropertySymbol> properties, string lastOperator)
        {
            return new[] { "Asc", "Desc" }.Select(direction => Item($"{identifierSoFar}{direction}", WellKnownTags.Keyword));
        }

        private static CompletionItem Item(string displayText, string tag)
            => CompletionItem.Create(
                displayText: displayText,
                sortText: $"0000_{displayText}",
                tags: ImmutableArray.Create(tag));
    }
}
