using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using DataQI.Analyzers.Query.Support;
using DataQI.Commons.Query;
using DataQI.Commons.Query.Ast;
using DataQI.Commons.Repository.Query;

namespace DataQI.Analyzers.Parity.Test
{
    internal static class ParityCheck
    {
        private static readonly QueryTokenizer Tokenizer = new QueryTokenizer();

        public static bool Matches(string name, IReadOnlyCollection<string> propertyNames, MethodInfo method)
        {
            var expected = ExpectedQuery.From(Tokenizer.Tokenize(name, propertyNames));
            if (expected == null)
                return false;

            try
            {
                var plan = QueryMethodParser.Parse(method);
                var predicate = Describe(plan.BuildPredicate(expected.Values));
                var orders = string.Join(",", plan.Orders.Select(order => $"{order.Direction}({order.PropertyName})"));

                return predicate == expected.Predicate && orders == expected.Orders;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static IReadOnlyList<string> Failures(IEnumerable<string> names, IReadOnlyCollection<string> propertyNames)
        {
            var distinct = names.Distinct().ToList();
            var methods = RuntimeMethods.Define(distinct);

            return distinct.Where(name => !Matches(name, propertyNames, methods[name])).ToList();
        }

        private static string Describe(ICriterion criterion)
        {
            switch (criterion)
            {
                case Junction junction:
                    return $"{junction.Kind}[{string.Join(",", junction.Members.Select(Describe))}]";
                case Not not:
                    return $"Not({Describe(not.Inner)})";
                case Comparison comparison:
                    return $"Comparison({comparison.PropertyName},{comparison.Kind})";
                case Between between:
                    return $"Between({between.PropertyName})";
                case In inCriterion:
                    return $"In({inCriterion.PropertyName})";
                case IsNull isNull:
                    return $"IsNull({isNull.PropertyName})";
                case TextMatch textMatch:
                    return $"TextMatch({textMatch.PropertyName},{textMatch.Kind})";
                default:
                    return criterion.GetType().Name;
            }
        }
    }
}
