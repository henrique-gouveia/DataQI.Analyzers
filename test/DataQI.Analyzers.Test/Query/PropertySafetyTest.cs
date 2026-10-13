using System;

using Xunit;

using DataQI.Analyzers.Query;

namespace DataQI.Analyzers.Test.Query
{
    public class PropertySafetyTest
    {
        [Theory]
        [InlineData("InStock")]
        [InlineData("LinkedIn")]
        [InlineData("LikeCount")]
        [InlineData("BetweenDates")]
        [InlineData("StartsWithFlag")]
        [InlineData("TermsAndConditions")]
        [InlineData("SalesAnd")]
        [InlineData("SalesOr")]
        [InlineData("ItemNot")]
        [InlineData("ItemIs")]
        [InlineData("TitleAsync")]
        [InlineData("OrderByDate")]
        public void TestRejectsUnsafePredicateNames(string name)
        {
            Assert.False(PropertySafety.IsSafe(name, PropertySafetyContext.Predicate));
        }

        [Theory]
        [InlineData("ItemIs")]
        [InlineData("ItemNot")]
        [InlineData("SalesOr")]
        [InlineData("SalesAnd")]
        public void TestRejectsReservedEndingsInBothContexts(string name)
        {
            Assert.False(PropertySafety.IsSafe(name, PropertySafetyContext.Predicate));
            Assert.False(PropertySafety.IsSafe(name, PropertySafetyContext.OrderBy));
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("CreatedBy")]
        [InlineData("Description")]
        [InlineData("Notes")]
        [InlineData("IsActive")]
        [InlineData("Index")]
        [InlineData("Order")]
        [InlineData("InvoiceId")]
        public void TestAcceptsSafePredicateNames(string name)
        {
            Assert.True(PropertySafety.IsSafe(name, PropertySafetyContext.Predicate));
        }

        [Theory]
        [InlineData("Desc")]
        [InlineData("Asc")]
        [InlineData("ProductDesc")]
        [InlineData("OrderByDate")]
        [InlineData("TitleAsync")]
        [InlineData("ItemIs")]
        [InlineData("ItemNot")]
        [InlineData("SalesOr")]
        [InlineData("SalesAnd")]
        public void TestRejectsUnsafeOrderByNames(string name)
        {
            Assert.False(PropertySafety.IsSafe(name, PropertySafetyContext.OrderBy));
        }

        [Theory]
        [InlineData("Description")]
        [InlineData("Name")]
        [InlineData("InStock")]
        [InlineData("TermsAndConditions")]
        public void TestAcceptsOrderByNamesThatOnlyBreakThePredicate(string name)
        {
            Assert.True(PropertySafety.IsSafe(name, PropertySafetyContext.OrderBy));
        }

        [Fact]
        public void TestRejectsEmptyName()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                PropertySafety.IsSafe(string.Empty, PropertySafetyContext.Predicate));

            Assert.Equal("Property name must not be null or empty", exception.Message);
        }
    }
}
