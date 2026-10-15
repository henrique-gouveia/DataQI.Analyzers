using System.Diagnostics.CodeAnalysis;

using DataQI.Analyzers.Completion.Support;
using DataQI.Analyzers.Query;

namespace DataQI.Analyzers.Completion.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class QueryTokenExtensions
    {
        public static ICompletionItemsBuilder GetCompletionItemsBuilder(this QueryToken lastToken, bool inOrderBy)
        {
            switch (lastToken.Kind)
            {
                case QueryTokenKind.Prefix:
                case QueryTokenKind.Junction:
                case QueryTokenKind.OrderBy:
                case QueryTokenKind.Direction:
                    return new PropertyCompletionItemsBuilder();

                case QueryTokenKind.Property:
                    return inOrderBy
                        ? (ICompletionItemsBuilder)new OrderDirectionCompletionItemsBuilder()
                        : new OperatorCompletionItemsBuilder();

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
