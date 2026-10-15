using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

using Xunit;

namespace DataQI.Analyzers.Test
{
    internal static class CompletionTestHost
    {
        private const string CaretMarker = "$$";

        public const string ProductClass = @"
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }";

        public const string DirectRepository =
            "public interface IProductRepository : DataQI.Commons.Repository.ICrudRepository<Product, int>";

        public static string Interface(string declaration, string member)
            => $"{ProductClass}\n    {declaration}\n    {{\n        {member}\n    }}";

        public static bool IsDataQIItem(CompletionItem item)
            => item.SortText != null && item.SortText.StartsWith("0000_", StringComparison.Ordinal);

        public static async Task<CompletionList> CompleteAsync(string body, CompletionTrigger trigger, bool expectDataQIItem)
        {
            var source = $@"
using System.Collections.Generic;

namespace DataQI.Commons.Repository
{{
    public interface ICrudRepository<TEntity, TId> {{ }}
}}

namespace CompletionSample
{{
{body}
}}
";
            var position = source.IndexOf(CaretMarker, StringComparison.Ordinal);
            Assert.True(position >= 0, "The body must contain the caret marker $$");
            source = source.Remove(position, CaretMarker.Length);

            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Concat(new[] { typeof(QueryMethodCompletionProvider).Assembly }));

            using var workspace = new AdhocWorkspace(hostServices);
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var projectInfo = ProjectInfo.Create(
                projectId, VersionStamp.Create(), "CompletionSample", "CompletionSample", LanguageNames.CSharp,
                metadataReferences: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            var solution = workspace.CurrentSolution
                .AddProject(projectInfo)
                .AddDocument(documentId, "IProductRepository.cs", SourceText.From(source));
            Assert.True(workspace.TryApplyChanges(solution));

            var document = workspace.CurrentSolution.GetDocument(documentId);
            var completionService = CompletionService.GetService(document);
            Assert.NotNull(completionService);

            CompletionList completions = null;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                completions = await completionService.GetCompletionsAsync(document, position, trigger);
                if (completions != null && (!expectDataQIItem || completions.ItemsList.Any(IsDataQIItem)))
                    break;
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            return completions;
        }
    }
}
