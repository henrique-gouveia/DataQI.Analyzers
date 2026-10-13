using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

using DataQI.Analyzers.Completion.Extensions;

namespace DataQI.Analyzers.Test.Completion
{
    public class TypeSymbolExtensionsTest
    {
        private static ITypeSymbol GetTypeOfFirstProperty(string classBody)
        {
            var source = $@"
using System;
public class Fixture {{ public {classBody} Value {{ get; set; }} }}";
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("Test", new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var model = compilation.GetSemanticModel(tree);
            var property = tree.GetRoot().DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>().Single();

            return ((IPropertySymbol)model.GetDeclaredSymbol(property)).Type;
        }

        [Fact]
        public void TestSuggestsStringOperators()
        {
            var operators = GetTypeOfFirstProperty("string").GetSuggestedOperators();

            Assert.Contains("StartingWith", operators);
            Assert.Contains("Like", operators);
            Assert.Contains("Null", operators);
            Assert.DoesNotContain("GreaterThan", operators);
        }

        [Fact]
        public void TestSuggestsDecimalOperators()
        {
            var operators = GetTypeOfFirstProperty("decimal").GetSuggestedOperators();

            Assert.Contains("GreaterThan", operators);
            Assert.Contains("Between", operators);
            Assert.DoesNotContain("StartingWith", operators);
            Assert.DoesNotContain("Null", operators);
        }

        [Fact]
        public void TestSuggestsDateTimeOperators()
        {
            var operators = GetTypeOfFirstProperty("DateTime").GetSuggestedOperators();

            Assert.Contains("Between", operators);
            Assert.Contains("LessThan", operators);
        }

        [Fact]
        public void TestSuggestsBoolOperators()
        {
            var operators = GetTypeOfFirstProperty("bool").GetSuggestedOperators();

            Assert.Equal(new[] { "Not" }, operators);
        }

        [Fact]
        public void TestSuggestsNullableIntOperatorsIncludingNullChecks()
        {
            var operators = GetTypeOfFirstProperty("int?").GetSuggestedOperators();

            Assert.Contains("GreaterThan", operators);
            Assert.Contains("Null", operators);
            Assert.Contains("NotNull", operators);
        }

        [Fact]
        public void TestSuggestsNullChecksForReferenceType()
        {
            var operators = GetTypeOfFirstProperty("object").GetSuggestedOperators();

            Assert.Contains("Not", operators);
            Assert.Contains("Null", operators);
            Assert.Contains("NotNull", operators);
        }

        [Fact]
        public void TestDoesNotSuggestOrderingOperatorsForGuid()
        {
            var operators = GetTypeOfFirstProperty("Guid").GetSuggestedOperators();

            Assert.DoesNotContain("GreaterThan", operators);
            Assert.DoesNotContain("Between", operators);
            Assert.Contains("In", operators);
            Assert.DoesNotContain("Null", operators);
        }

        [Theory]
        [InlineData("int", true)]
        [InlineData("int?", true)]
        [InlineData("string", true)]
        [InlineData("decimal", true)]
        [InlineData("char", true)]
        [InlineData("DateTime", true)]
        [InlineData("DateTimeOffset?", true)]
        [InlineData("TimeSpan", true)]
        [InlineData("Guid", true)]
        [InlineData("Guid?", true)]
        [InlineData("byte[]", true)]
        [InlineData("Color", true)]
        [InlineData("Color?", true)]
        [InlineData("object", false)]
        [InlineData("int[]", false)]
        [InlineData("List<string>", false)]
        [InlineData("IEnumerable<int>", false)]
        [InlineData("Holder", false)]
        public void TestIdentifiesScalarTypes(string typeName, bool expected)
        {
            Assert.Equal(expected, TestSymbols.Property(typeName).Type.IsScalar());
        }
    }
}
