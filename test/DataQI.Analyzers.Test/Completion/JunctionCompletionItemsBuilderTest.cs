using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Tags;

using Xunit;

using DataQI.Analyzers.Completion.Support;

namespace DataQI.Analyzers.Test.Completion
{
    public class JunctionCompletionItemsBuilderTest
    {
        [Fact]
        public void TestBuildsBothJunctionsCorrectly()
        {
            var builder = new JunctionCompletionItemsBuilder();

            var items = builder.Build("FindByNameEqual", new List<IPropertySymbol>(), null).ToList();

            Assert.Equal(new[] { "FindByNameEqualAnd", "FindByNameEqualOr" },
                items.Select(item => item.DisplayText));
            Assert.All(items, item => Assert.Contains(WellKnownTags.Keyword, item.Tags));
        }
    }
}
