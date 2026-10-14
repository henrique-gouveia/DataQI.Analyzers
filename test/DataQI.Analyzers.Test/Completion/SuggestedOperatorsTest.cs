using System.Linq;

using Xunit;

using DataQI.Analyzers.Completion.Extensions;

namespace DataQI.Analyzers.Test.Completion
{
    public class SuggestedOperatorsTest
    {
        private static string[] OperatorsOf(string typeName)
            => TestSymbols.Property(typeName).Type.GetSuggestedOperators().ToArray();

        [Theory]
        [InlineData("string")]
        [InlineData("bool")]
        [InlineData("bool?")]
        [InlineData("char")]
        [InlineData("char?")]
        [InlineData("byte[]")]
        [InlineData("Guid")]
        [InlineData("Guid?")]
        [InlineData("int")]
        [InlineData("int?")]
        [InlineData("DateTime")]
        [InlineData("DateTimeOffset?")]
        [InlineData("TimeSpan")]
        [InlineData("Color")]
        [InlineData("Color?")]
        public void TestEveryScalarSuggestsExplicitEquality(string typeName)
        {
            Assert.Single(OperatorsOf(typeName), op => op == "Equal");
        }

        [Fact]
        public void TestStringGetsTextOperatorsAndNullChecks()
        {
            Assert.Equal(
                new[] { "Equal", "Not", "StartingWith", "EndingWith", "Containing", "Like", "In", "NotIn", "Null", "NotNull" },
                OperatorsOf("string"));
        }

        [Theory]
        [InlineData("int")]
        [InlineData("decimal")]
        [InlineData("DateTime")]
        [InlineData("TimeSpan")]
        [InlineData("Color")]
        public void TestOrderedTypesGetComparisonOperatorsWithoutNullChecks(string typeName)
        {
            Assert.Equal(
                new[] { "Equal", "Not", "GreaterThan", "GreaterThanEqual", "LessThan", "LessThanEqual", "Between", "NotBetween", "In", "NotIn" },
                OperatorsOf(typeName));
        }

        [Theory]
        [InlineData("int?")]
        [InlineData("DateTime?")]
        [InlineData("Color?")]
        public void TestNullableOrderedTypesAlsoGetNullChecks(string typeName)
        {
            var operators = OperatorsOf(typeName);

            Assert.Contains("Between", operators);
            Assert.Equal(new[] { "Null", "NotNull" }, operators.Skip(operators.Length - 2));
        }

        [Fact]
        public void TestBoolGetsEqualityOperators()
        {
            Assert.Equal(new[] { "Equal", "Not" }, OperatorsOf("bool"));
        }

        [Fact]
        public void TestNullableBoolGetsNullChecks()
        {
            Assert.Equal(new[] { "Equal", "Not", "Null", "NotNull" }, OperatorsOf("bool?"));
        }

        [Fact]
        public void TestGuidHasNoOrderingOperators()
        {
            Assert.Equal(new[] { "Equal", "Not", "In", "NotIn" }, OperatorsOf("Guid"));
            Assert.Equal(new[] { "Equal", "Not", "In", "NotIn", "Null", "NotNull" }, OperatorsOf("Guid?"));
        }

        [Theory]
        [InlineData("char", new[] { "Equal", "Not" })]
        [InlineData("char?", new[] { "Equal", "Not", "Null", "NotNull" })]
        [InlineData("byte[]", new[] { "Equal", "Not", "Null", "NotNull" })]
        public void TestOtherScalarsGetEqualityOnly(string typeName, string[] expected)
        {
            Assert.Equal(expected, OperatorsOf(typeName));
        }
    }
}
