using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;

using Xunit;

using DataQI.Analyzers.Completion.Support;

namespace DataQI.Analyzers.Test.Completion
{
    public class OperatorCompletionItemsBuilderTest
    {
        private readonly OperatorCompletionItemsBuilder builder = new OperatorCompletionItemsBuilder();

        private static IPropertySymbol GetProperty(string typeName)
        {
            var source = $"public class Product {{ public {typeName} Value {{ get; set; }} }}";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("Test", new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var model = compilation.GetSemanticModel(tree);
            var property = tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().Single();

            return (IPropertySymbol)model.GetDeclaredSymbol(property);
        }

        [Fact]
        public void TestBuildsStringOperatorsAndJunctionsCorrectly()
        {
            var properties = new[] { GetProperty("string") };

            var items = builder.Build("FindByName", properties, null).ToList();

            Assert.Contains(items, item => item.DisplayText == "FindByNameStartingWith" && item.InlineDescription == "1 arg(s)");
            Assert.Contains(items, item => item.DisplayText == "FindByNameNull" && item.InlineDescription == "0 arg(s)");
            Assert.Contains(items, item => item.DisplayText == "FindByNameNotNull" && item.InlineDescription == "0 arg(s)");
            Assert.Contains(items, item => item.DisplayText == "FindByNameAnd");
            Assert.Contains(items, item => item.DisplayText == "FindByNameOr");
            Assert.All(items, item => Assert.Contains(WellKnownTags.Keyword, item.Tags));
        }

        [Fact]
        public void TestBuildsNumericOperatorsWithoutStringOperators()
        {
            var properties = new[] { GetProperty("decimal") };

            var items = builder.Build("FindByStock", properties, null).ToList();

            Assert.Contains(items, item => item.DisplayText == "FindByStockGreaterThan");
            Assert.Contains(items, item => item.DisplayText == "FindByStockNotBetween" && item.InlineDescription == "2 arg(s)");
            Assert.DoesNotContain(items, item => item.DisplayText == "FindByStockStartingWith");
        }

        [Fact]
        public void TestSuggestsOrderByWithoutArgumentDescription()
        {
            var items = builder.Build("FindByName", new[] { GetProperty("string") }, null).ToList();

            var orderBy = Assert.Single(items, item => item.DisplayText == "FindByNameOrderBy");
            Assert.True(string.IsNullOrEmpty(orderBy.InlineDescription));
        }
    }
}
