using System;
using System.Linq;
using System.Text.RegularExpressions;

using DataQI.Analyzers.Util;

namespace DataQI.Analyzers.Query
{
    /// <summary>
    /// Tells whether the runtime (QueryMethodParser, DataQI.Commons 4.0, or later) reads a property name back as the same
    /// property. The expressions mirror the parser's; the parity test keeps them in sync.
    /// The parser sees the property followed by the next token, so the name is evaluated with an uppercase
    /// continuation. The function is conservative on purpose: it may reject a harmless name, never accept a harmful one.
    /// </summary>
    public static class PropertySafety
    {
        private const string Continuation = "X";
        private static readonly string[] ReservedEndings = { "Async", "Is", "Not", "Or", "And" };
        private const string IsNotSmallLettersPattern = "(?!([a-z]))";
        private const string UppercaseOrEndPattern = "(?=[A-Z]|$)";
        private const string LessOrGreaterGroupPattern = "(Less|Greater)+Than(Equal)?";
        private const string LikeGroupPattern = "Contain(s|ing)|Like|((End|Start)+(s|ing)With)";
        private const string NotGroupPattern = "(Not)?(Null|Equals?|Between|In|" + LikeGroupPattern + ")";
        private const string TypePattern =
            "((Is)?(" + NotGroupPattern + "|" + LessOrGreaterGroupPattern + "))" + IsNotSmallLettersPattern;

        private static readonly Regex JunctionMatcher = new Regex("Or(?=[A-Z])|And(?=[A-Z])");
        private static readonly Regex NotMatcher = new Regex($"Not{IsNotSmallLettersPattern}");
        private static readonly Regex TypeMatcher = new Regex(TypePattern);
        private static readonly Regex OrderByMatcher = new Regex($"OrderBy{UppercaseOrEndPattern}");
        private static readonly Regex DirectionMatcher = new Regex($"(Asc|Desc){UppercaseOrEndPattern}");

        public static bool IsSafe(string propertyName, PropertySafetyContext context)
        {
            Assert.NotNullOrEmpty(propertyName, "Property name must not be null or empty");

            // QueryMethodParser recognizes the By prefix only before an ASCII uppercase letter.
            if (propertyName[0] < 'A' || propertyName[0] > 'Z')
                return false;

            var probe = $"{propertyName}{Continuation}";

            if (EndsWithReservedWord(propertyName) || OrderByMatcher.IsMatch(probe))
                return false;

            return context == PropertySafetyContext.OrderBy
                ? !DirectionMatcher.IsMatch(probe)
                : IsSafeInPredicate(propertyName, probe);
        }

        private static bool EndsWithReservedWord(string propertyName)
            => ReservedEndings.Any(ending => propertyName.EndsWith(ending, StringComparison.Ordinal));

        private static bool IsSafeInPredicate(string propertyName, string probe)
            => !JunctionMatcher.IsMatch(probe)
                && !NotMatcher.IsMatch(probe)
                && !TypeMatcher.IsMatch(probe);
    }
}
