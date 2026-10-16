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

        [Theory]
        [InlineData("DataQI.Commons.Repository.IRepository<Product, int>")]
        [InlineData("DataQI.Commons.Repository.ICrudRepository<Product, int>")]
        [InlineData("DataQI.Commons.Repository.IPagedRepository<Product, int>")]
        [InlineData("DataQI.Commons.Repository.ICrudRepository<Product, int>, DataQI.Commons.Repository.IPagedRepository<Product, int>")]
        public void TestResolvesIndependentRepositoryCapabilities(string baseTypes)
        {
            var (model, tree) = CompileModernRepository(baseTypes);

            var resolved = resolver.TryResolveEntityType(
                FindMethodNameToken(tree, "IProductRepository"), model, out var entityType);

            Assert.True(resolved);
            Assert.Equal("Product", entityType.Name);
        }

        [Theory]
        [InlineData("Other.IRepository<Product, int>")]
        [InlineData("Other.ICrudRepository<Product, int>")]
        [InlineData("DataQI.Commons.Repository.IRepository<Product>")]
        [InlineData("DataQI.Commons.Repository.ICrudRepository<Product>")]
        public void TestRejectsRepositoryLookalikes(string baseTypes)
        {
            var (model, tree) = CompileModernRepository(baseTypes);

            var resolved = resolver.TryResolveEntityType(
                FindMethodNameToken(tree, "IProductRepository"), model, out var entityType);

            Assert.False(resolved);
            Assert.Null(entityType);
        }

        private static (SemanticModel Model, SyntaxTree Tree) CompileModernRepository(string baseTypes)
        {
            var source = @"
namespace DataQI.Commons.Repository
{
    public interface IRepository<TEntity, TId> { }
    public interface ICrudRepository<TEntity, TId> : IRepository<TEntity, TId> { }
    public interface IPagedRepository<TEntity, TId> : IRepository<TEntity, TId> { }
    public interface IRepository<TEntity> { }
    public interface ICrudRepository<TEntity> { }
}
namespace Other
{
    public interface IRepository<TEntity, TId> { }
    public interface ICrudRepository<TEntity, TId> { }
}
public class Product { public string Name { get; set; } }
public interface IProductRepository : " + baseTypes + @"
{
    void FindByNa();
}";
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp7_3));
            var compilation = CSharpCompilation.Create("ModernRepository", new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

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
        public void TestGetQueryablePropertiesIncludesInheritedOnes()
        {
            var (model, tree) = Compile();
            var token = FindMethodNameToken(tree, "IProductRepositoryViaCrud");
            resolver.TryResolveEntityType(token, model, out var entityType);

            var properties = resolver.GetQueryableProperties(entityType).Select(p => p.Name);

            Assert.Contains("Id", properties);
            Assert.Contains("Name", properties);
        }

        [Fact]
        public void TestGetQueryablePropertiesExcludesStaticAndNonPublicOnes()
        {
            var (model, tree) = Compile();
            var token = FindMethodNameToken(tree, "IProductRepositoryViaCrud");
            resolver.TryResolveEntityType(token, model, out var entityType);

            var properties = resolver.GetQueryableProperties(entityType).Select(p => p.Name);

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
                resolver.GetQueryableProperties(null));

            Assert.Equal("EntityType must not be null", exception.Message);
        }

        private readonly RepositorySymbolResolver queryableResolver = new RepositorySymbolResolver();

        private static ITypeSymbol TypeOf(string source, string metadataName)
            => TestSymbols.Compile(source).GetTypeByMetadataName(metadataName);

        [Fact]
        public void TestReturnsOnlyPublicScalarPropertiesWithPublicGetter()
        {
            var type = TypeOf(@"
public class Mixed
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Hidden { private get; set; }
    public int WriteOnly { set { } }
    public int this[int index] { get { return index; } }
    public static int Counter { get; set; }
    [NotMapped] public string Ignored { get; set; }
    public List<string> Tags { get; set; }
    public Mixed Parent { get; set; }
    private int Secret { get; set; }
}", "Mixed");

            var names = queryableResolver.GetQueryableProperties(type).Select(p => p.Name).ToList();

            Assert.Equal(new[] { "Id", "Name" }, names);
        }

        [Fact]
        public void TestReturnsInheritedPropertiesFromTheBaseTypes()
        {
            var type = TypeOf(@"
public class Base { public int Id { get; set; } }
public class Derived : Base { public string Name { get; set; } }", "Derived");

            var names = queryableResolver.GetQueryableProperties(type).Select(p => p.Name).ToList();

            Assert.Equal(new[] { "Name", "Id" }, names);
        }

        [Fact]
        public void TestReturnsHiddenPropertyOnceWithDerivedType()
        {
            var type = TypeOf(@"
public class Base { public string Name { get; set; } }
public class Derived : Base { public new int Name { get; set; } }", "Derived");

            var properties = queryableResolver.GetQueryableProperties(type);

            var name = Assert.Single(properties);
            Assert.Equal(SpecialType.System_Int32, name.Type.SpecialType);
        }

        [Theory]
        [InlineData("public new List<string> Name { get; set; }")]
        [InlineData("[NotMapped] public new string Name { get; set; }")]
        public void TestHiddenUnqueryablePropertySuppressesBaseProperty(string declaration)
        {
            var type = TypeOf($@"
public class Base {{ public string Name {{ get; set; }} public int Id {{ get; set; }} }}
public class Derived : Base {{ {declaration} }}", "Derived");

            var names = queryableResolver.GetQueryableProperties(type).Select(p => p.Name).ToList();

            Assert.Equal(new[] { "Id" }, names);
        }

        [Fact]
        public void TestReturnsNoPropertiesForEntityWithoutScalars()
        {
            var type = TypeOf(@"
public class Order { public List<Order> Children { get; set; } public Order Parent { get; set; } }", "Order");

            Assert.Empty(queryableResolver.GetQueryableProperties(type));
        }

        [Fact]
        public void TestReturnsNoPropertiesForOpenGenericEntity()
        {
            var compilation = TestSymbols.Compile("public class Repo<T> { }");
            var typeParameter = compilation.GetTypeByMetadataName("Repo`1").TypeParameters[0];

            Assert.Empty(queryableResolver.GetQueryableProperties(typeParameter));
        }
    }
}
