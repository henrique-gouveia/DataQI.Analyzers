using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace DataQI.Analyzers.Completion
{
    public interface ICompletionItemsBuilder
    {
        IEnumerable<CompletionItem> Build(string identifierSoFar, IReadOnlyList<IPropertySymbol> properties, string lastOperator);
    }
}
