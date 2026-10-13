using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

using DataQI.Analyzers.Completion.Extensions;

namespace DataQI.Analyzers.Completion.Support
{
    public class PropertyCompletionItemsBuilder : ICompletionItemsBuilder
    {
        public IEnumerable<CompletionItem> Build(string identifierSoFar, IReadOnlyList<IPropertySymbol> properties, string lastOperator)
        {
            return properties.Select(property =>
            {
                var displayText = identifierSoFar + property.Name;
                return CompletionItem.Create(
                    displayText: displayText,
                    sortText: "0000_" + displayText,
                    inlineDescription: property.Type.ToDisplayString(),
                    tags: ImmutableArray.Create(WellKnownTags.Property));
            });
        }
    }
}
