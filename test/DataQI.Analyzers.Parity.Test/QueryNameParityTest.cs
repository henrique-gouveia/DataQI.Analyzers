using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using Xunit;

using DataQI.Analyzers.Completion.Extensions;
using DataQI.Analyzers.Query;
using DataQI.Analyzers.Query.Support;

namespace DataQI.Analyzers.Parity.Test
{
    public class QueryNameParityTest
    {
        private static readonly string[] Anchors = { "Name", "Age" };

        [Fact]
        public void TestRejectsDirectionWithoutAnOrderProperty()
        {
            var tokens = new QueryTokenizer().Tokenize("FindByNameOrderByDesc", new[] { "Name", "Desc" });

            Assert.Null(ExpectedQuery.From(tokens));
        }

        [Fact]
        public void TestMatchesFoundAndMultiplePredicateProperties()
        {
            var names = new[] { "FindByName", "FindByAge", "FindByNameAndAge" };

            Assert.Empty(ParityCheck.Failures(names, Anchors));
        }

        [Fact]
        public void TestReportsUnknownPropertyAsAMismatch()
        {
            var name = "FindByMissing";

            Assert.Equal(new[] { name }, ParityCheck.Failures(new[] { name }, Anchors));
        }

        [Fact]
        public void TestPropagatesInvalidPropertyNamesInput()
        {
            const string name = "FindByName";
            var method = RuntimeMethods.Define(new[] { name })[name];

            Assert.Throws<ArgumentException>(() => ParityCheck.Matches(name, null, method));
        }

        [Fact]
        public void TestEverySuggestedNameIsReadByTheRuntimeAsTheTokenizerReadsIt()
        {
            var properties = ParityEntities.Properties("Scalars");
            var names = SuggestedNames(properties).ToList();

            var failures = ParityCheck.Failures(names, properties.Select(p => p.Name).ToList());

            Assert.Empty(failures);
        }

        [Fact]
        public void TestEveryAliasTheTokenizerAcceptsIsReadByTheRuntimeAsEquivalent()
        {
            var names = QueryOperators.Aliases
                .SelectMany(entry => entry.Value)
                .SelectMany(alias => new[]
                {
                    $"FindByName{alias}",
                    $"FindByNameNot{alias}",
                    $"FindByName{alias}AndAge"
                });

            var failures = ParityCheck.Failures(names, Anchors);

            Assert.Empty(failures);
        }

        [Fact]
        public void TestOrderByWithDirectionsIsReadByTheRuntimeAsTheTokenizerReadsIt()
        {
            var names = new[]
            {
                "FindByNameOrderByAge",
                "FindByNameOrderByAgeDesc",
                "FindByNameOrderByAgeDescNameAsc",
                "FindByNameEqualAndAgeOrderByName",
                "FindByNameOrAgeBetweenOrderByNameDesc"
            };

            var failures = ParityCheck.Failures(names, Anchors);

            Assert.Empty(failures);
        }

        [Fact]
        public void TestTwoOrderPropertiesWithoutADirectionAreReadAsOneProperty()
        {
            // Completion must offer Asc/Desc here to keep adjacent order properties distinct.
            var failures = ParityCheck.Failures(new[] { "FindByNameOrderByAgeName" }, Anchors);

            Assert.NotEmpty(failures);
        }

        [Fact]
        public void TestEveryNameThatBreaksParityWasRejectedBySafety()
        {
            var hostile = ParityEntities.Properties("Hostile");
            var problems = new List<string>();

            foreach (var property in hostile.Where(p => !Anchors.Contains(p.Name)))
            {
                var propertyNames = Anchors.Concat(new[] { property.Name }).ToList();

                var predicateFailures = ParityCheck.Failures(PredicateNames(property), propertyNames);
                if (predicateFailures.Any() && PropertySafety.IsSafe(property.Name, PropertySafetyContext.Predicate))
                    problems.Add($"Predicate: {property.Name} accepted but breaks {string.Join(", ", predicateFailures)}");

                var orderFailures = ParityCheck.Failures(OrderNames(property.Name), propertyNames);
                if (orderFailures.Any() && PropertySafety.IsSafe(property.Name, PropertySafetyContext.OrderBy))
                    problems.Add($"OrderBy: {property.Name} accepted but breaks {string.Join(", ", orderFailures)}");
            }

            Assert.Empty(problems);
        }

        // Documented conservative rejection: the parser cuts at the first OrderBy, so a property that contains
        // OrderBy *after* the real one is harmless, but PropertySafety still rejects it.
        [Fact]
        public void TestRejectsHarmlessOrderByNameAfterTheRealOrderBy()
        {
            var failures = ParityCheck.Failures(
                new[] { "FindByNameOrderByPurchaseOrderBy" }, new[] { "Name", "PurchaseOrderBy" });

            Assert.Empty(failures);
            Assert.False(PropertySafety.IsSafe("PurchaseOrderBy", PropertySafetyContext.OrderBy));
        }

        private static IEnumerable<string> SuggestedNames(IReadOnlyList<IPropertySymbol> properties)
        {
            var predicate = properties.Where(p => PropertySafety.IsSafe(p.Name, PropertySafetyContext.Predicate)).ToList();
            var ordering = properties.Where(p => PropertySafety.IsSafe(p.Name, PropertySafetyContext.OrderBy)).ToList();

            foreach (var property in predicate)
            {
                var operators = property.Type.GetSuggestedOperators();
                var last = operators[operators.Count - 1];

                yield return $"FindBy{property.Name}";
                foreach (var op in operators)
                    yield return $"FindBy{property.Name}{op}";

                foreach (var other in predicate)
                {
                    yield return $"FindBy{property.Name}And{other.Name}";
                    yield return $"FindBy{property.Name}{last}And{other.Name}";
                    yield return $"FindBy{property.Name}Or{other.Name}";
                    yield return $"FindBy{property.Name}{last}Or{other.Name}";
                }

                foreach (var order in ordering)
                {
                    yield return $"FindBy{property.Name}OrderBy{order.Name}";
                    yield return $"FindBy{property.Name}{last}OrderBy{order.Name}Desc";
                    foreach (var second in ordering)
                        yield return $"FindBy{property.Name}OrderBy{order.Name}Asc{second.Name}Desc";
                }
            }
        }

        private static IEnumerable<string> PredicateNames(IPropertySymbol hostile)
        {
            var operators = new List<string> { string.Empty };
            operators.AddRange(hostile.Type.GetSuggestedOperators());

            foreach (var op in operators)
            {
                yield return $"FindBy{hostile.Name}{op}";
                yield return $"FindBy{hostile.Name}{op}AndAge";
                yield return $"FindBy{hostile.Name}{op}OrAge";
                yield return $"FindByNameAnd{hostile.Name}{op}";
                yield return $"FindByNameOr{hostile.Name}{op}";
                yield return $"FindBy{hostile.Name}{op}OrderByAge";
            }
        }

        private static IEnumerable<string> OrderNames(string hostile)
        {
            yield return $"FindByNameOrderBy{hostile}";
            yield return $"FindByNameOrderBy{hostile}Asc";
            yield return $"FindByNameOrderBy{hostile}DescAge";
            yield return $"FindByNameOrderByAgeDesc{hostile}";
            yield return $"FindByNameOrderByAgeAsc{hostile}Desc";
        }
    }
}
