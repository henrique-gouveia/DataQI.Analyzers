using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

using DataQI.Analyzers.Completion.Extensions;
using DataQI.Analyzers.Query;

namespace DataQI.Analyzers.Completion.Support
{
    public class OperatorCompletionItemsBuilder : ICompletionItemsBuilder
    {
        public IEnumerable<CompletionItem> Build(string identifierSoFar, IReadOnlyList<IPropertySymbol> properties, string lastOperator)
        {
            var lastProperty = properties.LastOrDefault();
            var operators = lastProperty == null
                ? System.Array.Empty<string>()
                : lastProperty.Type.GetSuggestedOperators();

            var suggestions = operators.Concat(new[] { "And", "Or" });

            return suggestions.Select(op =>
            {
                var displayText = identifierSoFar + op;
                var argCount = QueryOperators.ArgumentCounts.TryGetValue(op, out var count) ? count : 1;
                return CompletionItem.Create(
                    displayText: displayText,
                    sortText: $"0000_{displayText}",
                    inlineDescription: $"{argCount} arg(s)",
                    tags: ImmutableArray.Create(WellKnownTags.Keyword));
            });
        }
    }
}
