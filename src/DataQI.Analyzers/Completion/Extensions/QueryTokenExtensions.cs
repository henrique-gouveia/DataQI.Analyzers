using System.Diagnostics.CodeAnalysis;

using DataQI.Analyzers.Completion.Support;
using DataQI.Analyzers.Query;

namespace DataQI.Analyzers.Completion.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class QueryTokenExtensions
    {
        public static ICompletionItemsBuilder GetCompletionItemsBuilder(this QueryToken lastToken)
        {
            switch (lastToken.Kind)
            {
                case QueryTokenKind.Prefix:
                case QueryTokenKind.Junction:
                    return new PropertyCompletionItemsBuilder();

                case QueryTokenKind.Property:
                case QueryTokenKind.Not:
                    return new OperatorCompletionItemsBuilder();

                case QueryTokenKind.Operator:
                    return new JunctionCompletionItemsBuilder();

                case QueryTokenKind.Incomplete:
                    return null;

                default:
                    return null;
            }
        }
    }
}
