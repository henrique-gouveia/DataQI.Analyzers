using System.Linq;

using Xunit;

using DataQI.Analyzers.Completion.Support;

namespace DataQI.Analyzers.Test.Completion
{
    public class OrderDirectionCompletionItemsBuilderTest
    {
        [Fact]
        public void TestSuggestsOnlyDirectionsAfterAnOrderProperty()
        {
            var properties = new[] { TestSymbols.Property("int") };

            var items = new OrderDirectionCompletionItemsBuilder()
                .Build("FindByNameOrderByAge", properties, null).ToList();

            Assert.Contains(items, item => item.DisplayText == "FindByNameOrderByAgeAsc");
            Assert.Contains(items, item => item.DisplayText == "FindByNameOrderByAgeDesc");
            Assert.Equal(2, items.Count);
            Assert.DoesNotContain(items, item => item.DisplayText == "FindByNameOrderByAgeValue");
        }
    }
}
