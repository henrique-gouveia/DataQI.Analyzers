using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xunit;

using DataQI.Analyzers.Repository.Support;
using DataQI.Commons.Repository;

namespace DataQI.Analyzers.Parity.Test
{
    public class RepositorySymbolParityTest
    {
        public class Product
        {
            public string Name { get; set; }
        }

        public interface IProductRepository : ICrudRepository<Product, int>
        {
            IEnumerable<Product> FindByName(string name);
        }

        [Fact]
        public void TestPublishedCrudRepositoryResolvesPropertiesAcceptedByTheRuntimeParser()
        {
            var source = @"
public interface IQueries : DataQI.Analyzers.Parity.Test.RepositorySymbolParityTest.IProductRepository
{
    void FindByName();
}";
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp7_3));
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Concat(new[] { typeof(ICrudRepository<,>).Assembly.Location, typeof(IProductRepository).Assembly.Location })
                .Distinct()
                .Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create("PublishedRepositoryParity", new[] { tree }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
            var token = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single().Identifier;
            var resolver = new RepositorySymbolResolver();

            Assert.True(resolver.TryResolveEntityType(token, compilation.GetSemanticModel(tree), out var entityType));
            Assert.Equal(typeof(Product).FullName.Replace('+', '.'), entityType.ToDisplayString());
            var propertyNames = resolver.GetQueryableProperties(entityType).Select(p => p.Name).ToArray();
            Assert.Equal(new[] { nameof(Product.Name) }, propertyNames);
            Assert.True(ParityCheck.Matches(nameof(IProductRepository.FindByName), propertyNames,
                typeof(IProductRepository).GetMethod(nameof(IProductRepository.FindByName))));
        }
    }
}
