using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using DataQI.Analyzers.Query;
using DataQI.Analyzers.Query.Support;

namespace DataQI.Analyzers.Test.Query
{
    public class QueryTokenizerTest
    {
        private readonly QueryTokenizer tokenizer = new QueryTokenizer();

        private static readonly string[] BaseProperties = { "Name", "Age", "Stock", "Active" };

        [Fact]
        public void TestRejectsNullIdentifier()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                tokenizer.Tokenize(null, BaseProperties));

            Assert.Equal("Identifier must not be null or empty", exception.Message);
        }

        [Fact]
        public void TestRejectsEmptyIdentifier()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                tokenizer.Tokenize(string.Empty, BaseProperties));

            Assert.Equal("Identifier must not be null or empty", exception.Message);
        }

        [Fact]
        public void TestRejectsNullPropertyNames()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                tokenizer.Tokenize("FindByName", null));

            Assert.Equal("Property names must not be null", exception.Message);
        }

        [Fact]
        public void TestReturnsEmptyForNonFindByPrefix()
        {
            var tokens = tokenizer.Tokenize("SaveEntity", BaseProperties);

            Assert.Empty(tokens);
        }

        [Fact]
        public void TestTokenizesBareProperty()
        {
            var tokens = tokenizer.Tokenize("FindByName", BaseProperties);

            Assert.Collection(tokens,
                t => Assert.Equal(QueryTokenKind.Prefix, t.Kind),
                t => { Assert.Equal(QueryTokenKind.Property, t.Kind); Assert.Equal("Name", t.Text); });
        }

        [Theory]
        [InlineData("Between", "Between")]
        [InlineData("IsBetween", "Between")]
        [InlineData("Containing", "Containing")]
        [InlineData("IsContaining", "Containing")]
        [InlineData("Contains", "Containing")]
        [InlineData("EndingWith", "EndingWith")]
        [InlineData("IsEndingWith", "EndingWith")]
        [InlineData("EndsWith", "EndingWith")]
        [InlineData("Equal", "Equal")]
        [InlineData("IsEqual", "Equal")]
        [InlineData("GreaterThan", "GreaterThan")]
        [InlineData("IsGreaterThan", "GreaterThan")]
        [InlineData("GreaterThanEqual", "GreaterThanEqual")]
        [InlineData("IsGreaterThanEqual", "GreaterThanEqual")]
        [InlineData("In", "In")]
        [InlineData("IsIn", "In")]
        [InlineData("LessThan", "LessThan")]
        [InlineData("IsLessThan", "LessThan")]
        [InlineData("LessThanEqual", "LessThanEqual")]
        [InlineData("IsLessThanEqual", "LessThanEqual")]
        [InlineData("Like", "Like")]
        [InlineData("IsLike", "Like")]
        [InlineData("Null", "Null")]
        [InlineData("IsNull", "Null")]
        [InlineData("StartingWith", "StartingWith")]
        [InlineData("IsStartingWith", "StartingWith")]
        [InlineData("StartsWith", "StartingWith")]
        public void TestTokenizesEachOperatorAndAlias(string operatorText, string canonicalForm)
        {
            var tokens = tokenizer.Tokenize($"FindByName{operatorText}", BaseProperties);

            var operatorToken = tokens.Single(t => t.Kind == QueryTokenKind.Operator);
            Assert.Equal(canonicalForm, operatorToken.Text);
        }

        [Theory]
        [InlineData("Between")]
        [InlineData("Like")]
        [InlineData("Null")]
        public void TestTokenizesNotBeforeOperator(string operatorText)
        {
            var tokens = tokenizer.Tokenize($"FindByNameNot{operatorText}", BaseProperties);

            Assert.Contains(tokens, t => t.Kind == QueryTokenKind.Not);
            Assert.Contains(tokens, t => t.Kind == QueryTokenKind.Operator && t.Text == operatorText);
        }

        [Fact]
        public void TestTokenizesAndJunction()
        {
            var tokens = tokenizer.Tokenize("FindByNameAndAge", BaseProperties);

            Assert.Collection(tokens,
                t => Assert.Equal(QueryTokenKind.Prefix, t.Kind),
                t => Assert.Equal("Name", t.Text),
                t => Assert.Equal(QueryTokenKind.Junction, t.Kind),
                t => Assert.Equal("Age", t.Text));
        }

        [Fact]
        public void TestTokenizesOrJunction()
        {
            var tokens = tokenizer.Tokenize("FindByNameOrAge", BaseProperties);

            Assert.Contains(tokens, t => t.Kind == QueryTokenKind.Junction && t.Text == "Or");
        }

        [Fact]
        public void TestTokenizesMultipleConditionsWithMixedJunctions()
        {
            var tokens = tokenizer.Tokenize("FindByNameAndAgeOrStock", BaseProperties);

            Assert.Equal(new[] { "Name", "And", "Age", "Or", "Stock" },
                tokens.Skip(1).Select(t => t.Text));
        }

        [Fact]
        public void TestDiscardsTrailingAsyncSuffix()
        {
            var tokens = tokenizer.Tokenize("FindByNameAsync", BaseProperties);

            Assert.Contains(tokens, t => t.Kind == QueryTokenKind.AsyncSuffix && t.Text == "Async");
            Assert.DoesNotContain(tokens, t => t.Kind == QueryTokenKind.Property && t.Text.Contains("Async"));
        }

        [Fact]
        public void TestTreatsTrailingAsyncAsPartOfKnownProperty()
        {
            var tokens = tokenizer.Tokenize("FindByNameAsync", new[] { "NameAsync" });

            Assert.Contains(tokens, t => t.Kind == QueryTokenKind.Property && t.Text == "NameAsync");
            Assert.DoesNotContain(tokens, t => t.Kind == QueryTokenKind.AsyncSuffix);
        }

        [Theory]
        [InlineData("FindByOrderDate", "OrderDate")]
        [InlineData("FindByAndroidVersion", "AndroidVersion")]
        [InlineData("FindByInStock", "InStock")]
        [InlineData("FindByCreatedBy", "CreatedBy")]
        [InlineData("FindByLinkedIn", "LinkedIn")]
        public void TestResolvesKnownParserPitfallsAgainstRealPropertyNames(string identifier, string expectedProperty)
        {
            var properties = new[] { "OrderDate", "AndroidVersion", "InStock", "CreatedBy", "LinkedIn" };

            var tokens = tokenizer.Tokenize(identifier, properties);

            Assert.Contains(tokens, t => t.Kind == QueryTokenKind.Property && t.Text == expectedProperty);
        }

        [Fact]
        public void TestLongestPropertyNameWins()
        {
            var properties = new[] { "Name", "NameFull" };

            var tokens = tokenizer.Tokenize("FindByNameFull", properties);

            Assert.Contains(tokens, t => t.Kind == QueryTokenKind.Property && t.Text == "NameFull");
        }

        [Theory]
        [InlineData("FindBy")]
        [InlineData("FindByNa")]
        [InlineData("FindByNameStart")]
        [InlineData("FindByNameStartingWithAn")]
        public void TestMarksUnmatchedTailAsIncomplete(string identifier)
        {
            var properties = new[] { "Name" };

            var tokens = tokenizer.Tokenize(identifier, properties);

            Assert.Equal(QueryTokenKind.Incomplete, tokens.Last().Kind);
        }

        [Fact]
        public void TestTokenSpansAreCorrect()
        {
            var tokens = tokenizer.Tokenize("FindByName", BaseProperties);

            var propertyToken = tokens.Single(t => t.Kind == QueryTokenKind.Property);
            Assert.Equal(6, propertyToken.Start);
            Assert.Equal(4, propertyToken.Length);
            Assert.Equal("Name", "FindByName".Substring(propertyToken.Start, propertyToken.Length));
        }
    }
}
