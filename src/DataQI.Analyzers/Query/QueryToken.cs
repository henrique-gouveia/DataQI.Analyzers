namespace DataQI.Analyzers.Query
{
    public class QueryToken
    {
        public QueryToken(QueryTokenKind kind, string text, int start, int length)
        {
            Kind = kind;
            Text = text;
            Start = start;
            Length = length;
        }

        public QueryTokenKind Kind { get; }
        public string Text { get; }
        public int Start { get; }
        public int Length { get; }
    }
}
