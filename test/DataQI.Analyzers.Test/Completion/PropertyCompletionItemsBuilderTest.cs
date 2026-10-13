using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;

using Xunit;

using DataQI.Analyzers.Completion.Support;

namespace DataQI.Analyzers.Test.Completion
{
    public class PropertyCompletionItemsBuilderTest
    {
        private readonly PropertyCompletionItemsBuilder builder = new PropertyCompletionItemsBuilder();

        private static IReadOnlyList<IPropertySymbol> GetProperties()
        {
            const string source = "public class Product { public string Name { get; set; } public decimal Stock { get; set; } }";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("Test", new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var model = compilation.GetSemanticModel(tree);

            return tree.GetRoot().DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Select(property => (IPropertySymbol)model.GetDeclaredSymbol(property))
                .ToList();
        }

        [Fact]
        public void TestBuildsOneCompletionForEachPropertyCorrectly()
        {
            var items = builder.Build("FindBy", GetProperties(), null).ToList();

            Assert.Equal(2, items.Count);
            Assert.Contains(items, item => item.DisplayText == "FindByName" && item.InlineDescription == "string");
            Assert.Contains(items, item => item.DisplayText == "FindByStock" && item.InlineDescription == "decimal");
            Assert.All(items, item =>
            {
                Assert.Equal("0000_" + item.DisplayText, item.SortText);
                Assert.Contains(WellKnownTags.Property, item.Tags);
            });
        }

        [Fact]
        public void TestReturnsNoPropertyCompletionsWhenPropertiesAreEmpty()
        {
            var items = builder.Build("FindBy", new List<IPropertySymbol>(), null);

            Assert.Empty(items);
        }
    }
}
