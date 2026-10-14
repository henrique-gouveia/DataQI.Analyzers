using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Completion;

using Xunit;

namespace DataQI.Analyzers.Test
{
    public class QueryMethodCompletionProviderFlowTest
    {
        private static Task<CompletionList> CompleteMemberAsync(string member, char inserted, bool expectItem = true)
            => CompletionTestHost.CompleteAsync(
                CompletionTestHost.Interface(CompletionTestHost.DirectRepository, $"IEnumerable<Product> {member}$$"),
                CompletionTrigger.CreateInsertionTrigger(inserted),
                expectItem);

        [Theory]
        [InlineData("FindByName", 'e', "FindByNameEqual")]
        [InlineData("FindById", 'd', "FindByIdEqual")]
        [InlineData("FindByNameE", 'E', "FindByNameEqual")]
        [InlineData("FindByIdE", 'E', "FindByIdEqual")]
        public async Task TestSuggestsExplicitEquality(string member, char inserted, string expected)
        {
            var completions = await CompleteMemberAsync(member, inserted);

            var item = Assert.Single(completions.ItemsList, item => item.DisplayText == expected);
            Assert.Equal("1 arg(s)", item.InlineDescription);
        }

        [Fact]
        public async Task TestSuggestsOrderByAfterAProperty()
        {
            var completions = await CompleteMemberAsync("FindByName", 'e');

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameOrderBy");
            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameAnd");
        }

        [Fact]
        public async Task TestFiltersUnrelatedPropertiesForPartialName()
        {
            var completions = await CompleteMemberAsync("FindByNa", 'a');

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByName");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindById");
        }

        [Fact]
        public async Task TestKeepsMultipleMatchingPropertiesForPartialName()
        {
            var body = @"
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
    }
    public interface IProductRepository : DataQI.Commons.Repository.ICrudRepository<Product, int>
    {
        IEnumerable<Product> FindByN$$
    }";

            var completions = await CompletionTestHost.CompleteAsync(
                body, CompletionTrigger.CreateInsertionTrigger('N'), true);

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByName");
            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNickname");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindById");
        }

        [Fact]
        public async Task TestOffersNoDataQIItemForAnUnmatchedPartialName()
        {
            var positive = await CompleteMemberAsync("FindByNa", 'a');
            var missing = await CompleteMemberAsync("FindByZz", 'z', false);

            Assert.Contains(positive.ItemsList, item => item.DisplayText == "FindByName");
            Assert.DoesNotContain(missing.ItemsList, CompletionTestHost.IsDataQIItem);
        }

        [Fact]
        public async Task TestFiltersPartialOperatorName()
        {
            var completions = await CompleteMemberAsync("FindByNameSta", 'a');

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameStartingWith");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByNameOrderBy");
        }

        [Fact]
        public async Task TestSuggestsOrderByAfterAnOperator()
        {
            var completions = await CompleteMemberAsync("FindByNameStartingWith", 'h');

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameStartingWithOrderBy");
        }

        [Fact]
        public async Task TestSuggestsOnlyPropertiesAfterOrderBy()
        {
            var completions = await CompleteMemberAsync("FindByNameOrderBy", 'y');

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameOrderById");
            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByName");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByAnd");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByOrderBy");
        }

        [Fact]
        public async Task TestSuggestsOnlyDirectionsAfterAnOrderProperty()
        {
            var completions = await CompleteMemberAsync("FindByNameOrderByName", 'e');

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByNameAsc");
            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByNameDesc");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByNameId");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByNameAnd");
        }

        [Fact]
        public async Task TestSuggestsTheNextOrderPropertyAfterADirection()
        {
            var completions = await CompleteMemberAsync("FindByNameOrderByNameAsc", 'c');

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByNameAscId");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByNameOrderByNameAscOrderBy");
        }

        [Fact]
        public async Task TestHidesPropertiesTheRuntimeWouldMisread()
        {
            var body = @"
    public class Stocked
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool InStock { get; set; }
        public string TermsAndConditions { get; set; }
    }
    public interface IStockedRepository : DataQI.Commons.Repository.ICrudRepository<Stocked, int>
    {
        IEnumerable<Stocked> FindBy$$
    }";

            var completions = await CompletionTestHost.CompleteAsync(body, CompletionTrigger.CreateInsertionTrigger('y'), true);

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindByName");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByInStock");
            Assert.DoesNotContain(completions.ItemsList, item => item.DisplayText == "FindByTermsAndConditions");
        }

        [Fact]
        public async Task TestOffersNoPropertyItemsForEntityWithoutScalars()
        {
            var withScalars = await CompleteMemberAsync("FindBy", 'y');
            var body = @"
    public class Node
    {
        public Node Parent { get; set; }
        public List<Node> Children { get; set; }
    }
    public interface INodeRepository : DataQI.Commons.Repository.ICrudRepository<Node, int>
    {
        IEnumerable<Node> FindBy$$
    }";

            var withoutScalars = await CompletionTestHost.CompleteAsync(body, CompletionTrigger.CreateInsertionTrigger('y'), false);

            Assert.Contains(withScalars.ItemsList, item => item.DisplayText == "FindByName");
            Assert.DoesNotContain(withoutScalars.ItemsList, CompletionTestHost.IsDataQIItem);
        }
    }
}
