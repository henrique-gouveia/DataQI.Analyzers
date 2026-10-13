using System.Collections.Generic;

namespace DataQI.Analyzers.Query
{
    public interface IQueryTokenizer
    {
        IReadOnlyList<QueryToken> Tokenize(string identifier, IReadOnlyCollection<string> propertyNames);
    }
}
