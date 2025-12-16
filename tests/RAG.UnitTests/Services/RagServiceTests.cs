using FluentAssertions;
using RAG.Core.Models;
using RAG.Core.Services;
using RAG.UnitTests.Stubs;
using Xunit;

namespace RAG.UnitTests.Services;

public class RagServiceTests
{
    [Fact]
    public async Task AskAsync_WithSearchResults_ReturnsAnswerWithContextAndCitations()
    {
        // Arrange
        var searchResults = new List<VectorHit>
        {
            new VectorHit(
                "doc1_0",
                0.95f,
                new Dictionary<string, object>
                {
                    { "fileName", "doc1.txt" },
                    { "chunkIndex", 0 },
                    { "chunkText", "This is the first chunk of text." }
                }),
            new VectorHit(
                "doc1_1",
                0.87f,
                new Dictionary<string, object>
                {
                    { "fileName", "doc1.txt" },
                    { "chunkIndex", 1 },
                    { "chunkText", "This is the second chunk of text." }
                })
        };

        var stubVectorStore = new StubVectorStore(searchResults);
        var stubEmbeddingClient = new StubEmbeddingClient();
        var stubChatClient = new StubChatClient();

        var service = new RagService(stubEmbeddingClient, stubChatClient, stubVectorStore);

        // Act
        var result = await service.AskAsync("What is the content?");

        // Assert
        result.Should().NotBeNull();
        result.Answer.Should().NotBeNullOrEmpty();
        
        // The answer should contain the response based on context
        result.Answer.Should().Contain("Answer based on:");
        
        // The answer should contain references to the context
        result.Answer.Should().Contain("Context:");

        // Citations should match stub data
        result.Citations.Should().HaveCount(2);
        result.Citations[0].Should().Contain("doc1.txt");
        result.Citations[0].Should().Contain("first chunk");
        result.Citations[1].Should().Contain("doc1.txt");
        result.Citations[1].Should().Contain("second chunk");
    }

    [Fact]
    public async Task AskAsync_NoSearchResults_ReturnsAnswerWithEmptyCitations()
    {
        // Arrange - No search results
        var stubVectorStore = new StubVectorStore(new List<VectorHit>());
        var stubEmbeddingClient = new StubEmbeddingClient();
        var stubChatClient = new StubChatClient();

        var service = new RagService(stubEmbeddingClient, stubChatClient, stubVectorStore);

        // Act
        var result = await service.AskAsync("What is the content?");

        // Assert
        result.Should().NotBeNull();
        result.Answer.Should().NotBeNullOrEmpty();
        result.Answer.Should().Contain("Answer based on:");
        
        // No citations when no search results
        result.Citations.Should().BeEmpty();
    }

    [Fact]
    public async Task AskAsync_EmptyQuestion_ThrowsArgumentException()
    {
        // Arrange
        var stubVectorStore = new StubVectorStore();
        var stubEmbeddingClient = new StubEmbeddingClient();
        var stubChatClient = new StubChatClient();

        var service = new RagService(stubEmbeddingClient, stubChatClient, stubVectorStore);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await service.AskAsync("");
        });
    }
}
