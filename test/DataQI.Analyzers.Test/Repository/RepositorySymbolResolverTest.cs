using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xunit;

using DataQI.Analyzers.Repository.Support;

namespace DataQI.Analyzers.Test.Repository
{
    public class RepositorySymbolResolverTest
    {
        private readonly RepositorySymbolResolver resolver = new RepositorySymbolResolver();

        private const string StubSource = @"
namespace DataQI.Commons.Repository
{
    public interface ICrudRepository<TEntity, TId> { }
}

namespace DataQI.EntityFrameworkCore.Repository
{
    public interface IEntityRepository<TEntity, TId> : DataQI.Commons.Repository.ICrudRepository<TEntity, TId> { }
}

public class BaseProduct
{
    public int Id { get; set; }
}

public class Product : BaseProduct
{
    public string Name { get; set; }
    public static string GlobalName { get; set; }
    private string Secret { get; set; }
}

public interface IProductRepositoryViaCrud : DataQI.Commons.Repository.ICrudRepository<Product, int>
{
    void FindByNa();
}

public interface IProductRepositoryViaEntity : DataQI.EntityFrameworkCore.Repository.IEntityRepository<Product, int>
{
    void FindByNa();
}

public interface INotARepository
{
    void FindByNa();
}
";

        private static (SemanticModel Model, SyntaxTree Tree) Compile()
        {
            var tree = CSharpSyntaxTree.ParseText(StubSource);
            var compilation = CSharpCompilation.Create("Test",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            return (compilation.GetSemanticModel(tree), tree);
        }

        private static SyntaxToken FindMethodNameToken(SyntaxTree tree, string interfaceName)
        {
            var root = tree.GetRoot();
            var method = root.DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .Single(i => i.Identifier.Text == interfaceName)
                .DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            return method.Identifier;
        }

        [Fact]
        public void TestResolvesEntityTypeViaDirectCrudRepository()
        {
            var (model, tree) = Compile();
            var token = FindMethodNameToken(tree, "IProductRepositoryViaCrud");

            var resolved = resolver.TryResolveEntityType(token, model, out var entityType);

            Assert.True(resolved);
            Assert.Equal("Product", entityType.Name);
        }

        [Fact]
        public void TestResolvesEntityTypeViaInheritedProviderInterface()
        {
            var (model, tree) = Compile();
            var token = FindMethodNameToken(tree, "IProductRepositoryViaEntity");

            var resolved = resolver.TryResolveEntityType(token, model, out var entityType);

            Assert.True(resolved);
            Assert.Equal("Product", entityType.Name);
        }

        [Fact]
        public void TestFailsForNonRepositoryInterface()
        {
            var (model, tree) = Compile();
            var token = FindMethodNameToken(tree, "INotARepository");

            var resolved = resolver.TryResolveEntityType(token, model, out var entityType);

            Assert.False(resolved);
            Assert.Null(entityType);
        }

        [Fact]
        public void TestGetPublicInstancePropertiesIncludesInheritedOnes()
        {
            var (model, tree) = Compile();
            var token = FindMethodNameToken(tree, "IProductRepositoryViaCrud");
            resolver.TryResolveEntityType(token, model, out var entityType);

            var properties = resolver.GetPublicInstanceProperties(entityType).Select(p => p.Name);

            Assert.Contains("Id", properties);
            Assert.Contains("Name", properties);
        }

        [Fact]
        public void TestGetPublicInstancePropertiesExcludesStaticAndNonPublicOnes()
        {
            var (model, tree) = Compile();
            var token = FindMethodNameToken(tree, "IProductRepositoryViaCrud");
            resolver.TryResolveEntityType(token, model, out var entityType);

            var properties = resolver.GetPublicInstanceProperties(entityType).Select(p => p.Name);

            Assert.DoesNotContain("GlobalName", properties);
            Assert.DoesNotContain("Secret", properties);
        }

        [Fact]
        public void TestRejectsNullSemanticModel()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                resolver.TryResolveEntityType(default, null, out _));

            Assert.Equal("SemanticModel must not be null", exception.Message);
        }

        [Fact]
        public void TestRejectsNullEntityType()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                resolver.GetPublicInstanceProperties(null));

            Assert.Equal("EntityType must not be null", exception.Message);
        }
    }
}
