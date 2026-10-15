using System.Collections.Generic;
using System.Linq;

using DataQI.Analyzers.Query;

namespace DataQI.Analyzers.Parity.Test
{
    internal sealed class ExpectedQuery
    {
        public string Predicate { get; private set; }
        public string Orders { get; private set; }
        public object[] Values { get; private set; }

        public static ExpectedQuery From(IReadOnlyList<QueryToken> tokens)
        {
            var groups = new List<List<string>> { new List<string>() };
            var orders = new List<KeyValuePair<string, string>>();
            var values = new List<object>();
            string property = null;
            string op = null;
            var negated = false;
            var inOrderBy = false;

            void Flush()
            {
                if (property == null)
                    return;

                groups[groups.Count - 1].Add(Describe(property, op, negated, values));
                property = null;
                op = null;
                negated = false;
            }

            foreach (var token in tokens)
            {
                switch (token.Kind)
                {
                    case QueryTokenKind.Prefix:
                    case QueryTokenKind.AsyncSuffix:
                        break;
                    case QueryTokenKind.Incomplete:
                        return null;
                    case QueryTokenKind.Property when inOrderBy:
                        orders.Add(new KeyValuePair<string, string>(token.Text, "Asc"));
                        break;
                    case QueryTokenKind.Property:
                        Flush();
                        property = token.Text;
                        break;
                    case QueryTokenKind.Not:
                        negated = true;
                        break;
                    case QueryTokenKind.Operator:
                        op = token.Text;
                        break;
                    case QueryTokenKind.Junction:
                        Flush();
                        if (token.Text == "Or")
                            groups.Add(new List<string>());
                        break;
                    case QueryTokenKind.OrderBy:
                        Flush();
                        inOrderBy = true;
                        break;
                    case QueryTokenKind.Direction:
                        if (orders.Count == 0)
                            return null;
                        var last = orders.Count - 1;
                        orders[last] = new KeyValuePair<string, string>(orders[last].Key, token.Text);
                        break;
                }
            }

            Flush();

            return new ExpectedQuery
            {
                Predicate = $"Or[{string.Join(",", groups.Select(group => $"And[{string.Join(",", group)}]"))}]",
                Orders = string.Join(",", orders.Select(order => $"{order.Value}({order.Key})")),
                Values = values.ToArray()
            };
        }

        private static string Describe(string property, string op, bool negated, List<object> values)
        {
            string criterion;
            switch (op)
            {
                case "Between":
                    values.Add(1);
                    values.Add(2);
                    criterion = $"Between({property})";
                    break;
                case "In":
                    values.Add(new object[] { 1 });
                    criterion = $"In({property})";
                    break;
                case "Null":
                    criterion = $"IsNull({property})";
                    break;
                case "Containing":
                case "StartingWith":
                case "EndingWith":
                case "Like":
                    values.Add("x");
                    criterion = $"TextMatch({property},{op})";
                    break;
                case "GreaterThan":
                case "GreaterThanEqual":
                case "LessThan":
                case "LessThanEqual":
                    values.Add(1);
                    criterion = $"Comparison({property},{op})";
                    break;
                default:
                    values.Add(1);
                    criterion = $"Comparison({property},Equal)";
                    break;
            }

            return negated ? $"Not({criterion})" : criterion;
        }
    }
}
