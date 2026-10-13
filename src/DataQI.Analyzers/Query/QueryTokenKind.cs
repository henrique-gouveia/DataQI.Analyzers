namespace DataQI.Analyzers.Query
{
    public enum QueryTokenKind
    {
        Prefix,
        Property,
        Not,
        Operator,
        Junction,
        OrderBy,
        Direction,
        AsyncSuffix,
        Incomplete
    }
}
