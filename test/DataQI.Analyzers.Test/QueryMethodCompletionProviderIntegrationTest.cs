using System;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

using Xunit;

namespace DataQI.Analyzers.Test
{
    public class QueryMethodCompletionProviderIntegrationTest
    {
        private const string SampleSource = @"
using System.Collections.Generic;

public interface ICrudRepository<TEntity, TId> { }

namespace DataQI.Commons.Repository
{
    public interface ICrudRepository<TEntity, TId> { }
}

namespace CompletionSample
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IProductRepository : DataQI.Commons.Repository.ICrudRepository<Product, int>
    {
        IEnumerable<Product> FindByNa(
    }
}
";

        [Fact]
        public void TestSampleCompletionPositionResolvesMethodIdentifier()
        {
            var position = SampleSource.IndexOf("FindByNa") + "FindByNa".Length;
            var token = CSharpSyntaxTree.ParseText(SampleSource).GetRoot().FindToken(position - 1);

            Assert.Equal("FindByNa", token.Text);
            Assert.IsType<MethodDeclarationSyntax>(token.Parent);
        }

        [Fact]
        public async Task TestProviderAssemblyLoadsAndContributesCompletions()
        {
            var completions = await GetCompletionsAsync(SampleSource, "FindByNa", true);

            Assert.NotNull(completions);
            Assert.Contains(completions.ItemsList, i => i.DisplayText == "FindByName");
        }

        [Fact]
        public async Task TestProviderContributesMultiplePropertyCompletions()
        {
            var source = SampleSource.Replace("FindByNa", "FindBy");

            var completions = await GetCompletionsAsync(source, "FindBy", true);

            Assert.Contains(completions.ItemsList, i => i.DisplayText == "FindById");
            Assert.Contains(completions.ItemsList, i => i.DisplayText == "FindByName");
        }

        [Fact]
        public async Task TestProviderDoesNotContributeCompletionsForNonRepository()
        {
            var source = SampleSource.Replace(
                "IProductRepository : DataQI.Commons.Repository.ICrudRepository<Product, int>",
                "IProductRepository");

            var completions = await GetCompletionsAsync(source, "FindByNa", false);

            Assert.DoesNotContain(completions.ItemsList,
                item => item.SortText != null && item.SortText.StartsWith("0000_"));
        }

        [Theory]
        [InlineData("IRepository<Product, int>")]
        [InlineData("ICrudRepository<Product, int>")]
        [InlineData("IPagedRepository<Product, int>")]
        [InlineData("ICrudRepository<Product, int>, DataQI.Commons.Repository.IPagedRepository<Product, int>")]
        public async Task TestProviderContributesCompletionsForIndependentCapabilities(string baseTypes)
        {
            var source = SampleSource.Replace(
                "    public interface ICrudRepository<TEntity, TId> { }",
                @"    public interface IRepository<TEntity, TId> { }
    public interface ICrudRepository<TEntity, TId> : IRepository<TEntity, TId> { }
    public interface IPagedRepository<TEntity, TId> : IRepository<TEntity, TId> { }")
                .Replace("IProductRepository : DataQI.Commons.Repository.ICrudRepository<Product, int>",
                    "IProductRepository : DataQI.Commons.Repository." + baseTypes);

            var completions = await GetCompletionsAsync(source, "FindByNa", true);

            Assert.NotNull(completions);
            Assert.Contains(completions.ItemsList,
                item => item.DisplayText == "FindByName" && CompletionTestHost.IsDataQIItem(item));
        }

        [Fact]
        public void TestProviderTriggerRulesCorrectly()
        {
            var provider = new QueryMethodCompletionProvider();
            var text = SourceText.From("FindBy");

            Assert.True(provider.ShouldTriggerCompletion(text, text.Length, CompletionTrigger.Invoke, null));
            Assert.True(provider.ShouldTriggerCompletion(text, text.Length,
                CompletionTrigger.CreateInsertionTrigger('y'), null));
            Assert.False(provider.ShouldTriggerCompletion(text, text.Length,
                CompletionTrigger.CreateInsertionTrigger('1'), null));
        }

        private static async Task<CompletionList> GetCompletionsAsync(string source, string identifier, bool waitForDataQIItem)
        {
            var providerAssemblyPath = typeof(QueryMethodCompletionProvider).Assembly.Location;
            Assert.True(File.Exists(providerAssemblyPath),
                $"Provider assembly not found at {providerAssemblyPath} — build the src project first.");

            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies
                    .Concat(new[] { typeof(QueryMethodCompletionProvider).Assembly }));

            using var workspace = new AdhocWorkspace(hostServices);
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var projectInfo = ProjectInfo.Create(
                projectId, VersionStamp.Create(), "CompletionSample", "CompletionSample", LanguageNames.CSharp,
                metadataReferences: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                });

            var solution = workspace.CurrentSolution
                .AddProject(projectInfo)
                .AddDocument(documentId, "IProductRepository.cs", SourceText.From(source));

            Assert.True(workspace.TryApplyChanges(solution));

            var document = workspace.CurrentSolution.GetDocument(documentId);
            var position = source.IndexOf(identifier) + identifier.Length;

            var completionService = CompletionService.GetService(document);
            Assert.NotNull(completionService);

            CompletionList completions = null;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                completions = await completionService.GetCompletionsAsync(document, position);
                if (completions != null && (!waitForDataQIItem || HasDataQIItem(completions))) break;
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            return completions;
        }

        private static bool HasDataQIItem(CompletionList completions)
            => completions.ItemsList.Any(i => i.SortText != null && i.SortText.StartsWith("0000_"));
    }
}
