using System;
using System.Collections.Generic;
using System.Linq;

using DataQI.Analyzers.Util;

namespace DataQI.Analyzers.Query.Support
{
    public class QueryTokenizer : IQueryTokenizer
    {
        private const string PrefixText = "FindBy";
        private const string AsyncSuffixText = "Async";
        private const string NotText = "Not";
        private const string AndText = "And";
        private const string OrText = "Or";

        public IReadOnlyList<QueryToken> Tokenize(string identifier, IReadOnlyCollection<string> propertyNames)
        {
            Assert.NotNullOrEmpty(identifier, "Identifier must not be null or empty");
            Assert.NotNull(propertyNames, "Property names must not be null");

            if (!identifier.StartsWith(PrefixText, StringComparison.Ordinal))
                return new List<QueryToken>();

            var tokens = new List<QueryToken> { new QueryToken(QueryTokenKind.Prefix, PrefixText, 0, PrefixText.Length) };

            var body = identifier.Substring(PrefixText.Length);
            var bodyStart = PrefixText.Length;

            var asyncIsPartOfProperty = propertyNames.Any(p =>
                p.EndsWith(AsyncSuffixText, StringComparison.Ordinal) &&
                body.EndsWith(p, StringComparison.Ordinal));
            var hasAsyncSuffix = body.EndsWith(AsyncSuffixText, StringComparison.Ordinal) &&
                !asyncIsPartOfProperty;
            var asyncSuffixToken = hasAsyncSuffix
                ? new QueryToken(QueryTokenKind.AsyncSuffix, AsyncSuffixText,
                    bodyStart + body.Length - AsyncSuffixText.Length, AsyncSuffixText.Length)
                : null;
            if (hasAsyncSuffix)
                body = body.Substring(0, body.Length - AsyncSuffixText.Length);

            var orderedProperties = propertyNames.OrderByDescending(p => p.Length).ToList();
            var position = 0;

            if (body.Length == 0)
                tokens.Add(new QueryToken(QueryTokenKind.Incomplete, string.Empty, bodyStart, 0));

            while (position < body.Length)
            {
                var remaining = body.Substring(position);

                var property = orderedProperties.FirstOrDefault(p =>
                    remaining.StartsWith(p, StringComparison.Ordinal));
                if (property != null)
                {
                    tokens.Add(new QueryToken(QueryTokenKind.Property, property, bodyStart + position, property.Length));
                    position += property.Length;
                    continue;
                }

                if (remaining.StartsWith(NotText, StringComparison.Ordinal) && !IsFollowedByLowercase(remaining, NotText.Length))
                {
                    tokens.Add(new QueryToken(QueryTokenKind.Not, NotText, bodyStart + position, NotText.Length));
                    position += NotText.Length;
                    continue;
                }

                var operatorMatch = TryMatchOperator(remaining);
                if (operatorMatch != null)
                {
                    tokens.Add(new QueryToken(QueryTokenKind.Operator, operatorMatch.Canonical, bodyStart + position, operatorMatch.MatchedLength));
                    position += operatorMatch.MatchedLength;
                    continue;
                }

                if (remaining.StartsWith(AndText, StringComparison.Ordinal) && !IsFollowedByLowercase(remaining, AndText.Length))
                {
                    tokens.Add(new QueryToken(QueryTokenKind.Junction, AndText, bodyStart + position, AndText.Length));
                    position += AndText.Length;
                    continue;
                }

                if (remaining.StartsWith(OrText, StringComparison.Ordinal) && !IsFollowedByLowercase(remaining, OrText.Length))
                {
                    tokens.Add(new QueryToken(QueryTokenKind.Junction, OrText, bodyStart + position, OrText.Length));
                    position += OrText.Length;
                    continue;
                }

                tokens.Add(new QueryToken(QueryTokenKind.Incomplete, remaining, bodyStart + position, remaining.Length));
                position = body.Length;
            }

            if (asyncSuffixToken != null)
                tokens.Add(asyncSuffixToken);

            return tokens;
        }

        private static bool IsFollowedByLowercase(string text, int indexAfterMatch)
            => indexAfterMatch < text.Length && char.IsLower(text[indexAfterMatch]);

        private static OperatorMatch TryMatchOperator(string remaining)
        {
            var hasIsPrefix = remaining.StartsWith("Is", StringComparison.Ordinal);
            var candidate = hasIsPrefix ? remaining.Substring(2) : remaining;

            var matches = QueryOperators.Aliases
                .SelectMany(entry => entry.Value.Select(alias => new
                {
                    Canonical = entry.Key,
                    Alias = hasIsPrefix && alias.StartsWith("Is", StringComparison.Ordinal)
                        ? alias.Substring(2)
                        : alias
                }))
                .Select(match => new
                {
                    match.Canonical,
                    match.Alias,
                    MatchedLength = (hasIsPrefix ? 2 : 0) + match.Alias.Length
                })
                .OrderByDescending(match => match.MatchedLength);

            foreach (var match in matches)
            {
                if (!candidate.StartsWith(match.Alias, StringComparison.Ordinal))
                    continue;

                if (IsFollowedByLowercase(remaining, match.MatchedLength))
                    continue;

                return new OperatorMatch(match.Canonical, match.MatchedLength);
            }

            return null;
        }

        private class OperatorMatch
        {
            public OperatorMatch(string canonical, int matchedLength)
            {
                Canonical = canonical;
                MatchedLength = matchedLength;
            }

            public string Canonical { get; }
            public int MatchedLength { get; }
        }
    }
}
