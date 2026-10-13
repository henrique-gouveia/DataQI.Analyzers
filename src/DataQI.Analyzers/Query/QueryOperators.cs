using System.Collections.Generic;

namespace DataQI.Analyzers.Query
{
    public static class QueryOperators
    {
        public static readonly IReadOnlyDictionary<string, string[]> Aliases = new Dictionary<string, string[]>
        {
            { "Between", new[] { "Between", "IsBetween" } },
            { "Containing", new[] { "Containing", "IsContaining", "Contains" } },
            { "EndingWith", new[] { "EndingWith", "IsEndingWith", "EndsWith" } },
            { "Equal", new[] { "Equal", "IsEqual" } },
            { "GreaterThan", new[] { "GreaterThan", "IsGreaterThan" } },
            { "GreaterThanEqual", new[] { "GreaterThanEqual", "IsGreaterThanEqual" } },
            { "In", new[] { "In", "IsIn" } },
            { "LessThan", new[] { "LessThan", "IsLessThan" } },
            { "LessThanEqual", new[] { "LessThanEqual", "IsLessThanEqual" } },
            { "Like", new[] { "Like", "IsLike" } },
            { "Null", new[] { "Null", "IsNull" } },
            { "StartingWith", new[] { "StartingWith", "IsStartingWith", "StartsWith" } },
        };

        public static readonly IReadOnlyDictionary<string, int> ArgumentCounts = new Dictionary<string, int>
        {
            { "Between", 2 },
            { "Containing", 1 },
            { "EndingWith", 1 },
            { "Equal", 1 },
            { "GreaterThan", 1 },
            { "GreaterThanEqual", 1 },
            { "In", 1 },
            { "LessThan", 1 },
            { "LessThanEqual", 1 },
            { "Like", 1 },
            { "Null", 0 },
            { "NotBetween", 2 },
            { "NotIn", 1 },
            { "NotNull", 0 },
            { "StartingWith", 1 },
        };
    }
}
