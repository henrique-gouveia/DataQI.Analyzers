using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Completion;

using Xunit;

namespace DataQI.Analyzers.Test
{
    public class QueryMethodCompletionProviderTriggerTest
    {
        [Theory]
        [InlineData("F", 'F')]
        [InlineData("f", 'f')]
        [InlineData("Fi", 'i')]
        [InlineData("Find", 'd')]
        [InlineData("FindB", 'B')]
        public async Task TestSuggestsFindByWhileTypingThePrefix(string typed, char inserted)
        {
            var body = CompletionTestHost.Interface(
                CompletionTestHost.DirectRepository, $"IEnumerable<Product> {typed}$$");

            var completions = await CompletionTestHost.CompleteAsync(
                body, CompletionTrigger.CreateInsertionTrigger(inserted), true);

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindBy");
        }

        [Fact]
        public async Task TestDoesNotSuggestFindByForOtherTypedText()
        {
            var body = CompletionTestHost.Interface(
                CompletionTestHost.DirectRepository, "IEnumerable<Product> Get$$");

            var completions = await CompletionTestHost.CompleteAsync(
                body, CompletionTrigger.CreateInsertionTrigger('t'), false);

            Assert.DoesNotContain(completions.ItemsList, CompletionTestHost.IsDataQIItem);
        }

        [Theory]
        [InlineData("IEnumerable<Product> $$")]
        [InlineData("Product $$")]
        public async Task TestSuggestsFindByOnManualInvokeAfterTheReturnType(string member)
        {
            var body = CompletionTestHost.Interface(CompletionTestHost.DirectRepository, member);

            var completions = await CompletionTestHost.CompleteAsync(body, CompletionTrigger.Invoke, true);

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindBy");
        }

        [Fact]
        public async Task TestDoesNotSuggestFindByAtAnEmptyMemberPosition()
        {
            var positive = CompletionTestHost.Interface(
                CompletionTestHost.DirectRepository, "IEnumerable<Product> $$");
            var empty = CompletionTestHost.Interface(CompletionTestHost.DirectRepository, "$$");

            var positiveCompletions = await CompletionTestHost.CompleteAsync(positive, CompletionTrigger.Invoke, true);
            var emptyCompletions = await CompletionTestHost.CompleteAsync(empty, CompletionTrigger.Invoke, false);

            Assert.Contains(positiveCompletions.ItemsList, item => item.DisplayText == "FindBy");
            Assert.DoesNotContain(emptyCompletions.ItemsList, CompletionTestHost.IsDataQIItem);
        }

        [Fact]
        public async Task TestSuggestsFindByInAnIndirectInterface()
        {
            var body = CompletionTestHost.ProductClass + @"
    public interface IBase<T> : DataQI.Commons.Repository.ICrudRepository<T, int> { }
    public interface IMiddle<T> : IBase<T> { }
    public interface IProductRepository : IMiddle<Product>
    {
        IEnumerable<Product> F$$
    }";

            var completions = await CompletionTestHost.CompleteAsync(
                body, CompletionTrigger.CreateInsertionTrigger('F'), true);

            Assert.Contains(completions.ItemsList, item => item.DisplayText == "FindBy");
        }

        [Fact]
        public async Task TestDoesNotSuggestFindByOutsideRepositoryInterfaces()
        {
            var repository = CompletionTestHost.Interface(
                CompletionTestHost.DirectRepository, "IEnumerable<Product> F$$");
            var plain = CompletionTestHost.Interface("public interface IProductRepository", "IEnumerable<Product> F$$");
            var trigger = CompletionTrigger.CreateInsertionTrigger('F');

            var positive = await CompletionTestHost.CompleteAsync(repository, trigger, true);
            var negative = await CompletionTestHost.CompleteAsync(plain, trigger, false);

            Assert.Contains(positive.ItemsList, item => item.DisplayText == "FindBy");
            Assert.DoesNotContain(negative.ItemsList, CompletionTestHost.IsDataQIItem);
        }
    }
}
