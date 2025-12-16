using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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

        var service = new RagService(
            stubEmbeddingClient, 
            stubChatClient, 
            stubVectorStore,
            0.0,
            512,
            NullLogger<RagService>.Instance);

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

        var service = new RagService(
            stubEmbeddingClient, 
            stubChatClient, 
            stubVectorStore,
            0.0,
            512,
            NullLogger<RagService>.Instance);

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

        var service = new RagService(
            stubEmbeddingClient, 
            stubChatClient, 
            stubVectorStore,
            0.0,
            512,
            NullLogger<RagService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await service.AskAsync("");
        });
    }

    [Fact]
    public async Task AskAsync_ExtractsMetadataWithLowercaseKeys()
    {
        // Arrange
        var searchResults = new List<VectorHit>
        {
            new VectorHit(
                "1",
                0.95f,
                new Dictionary<string, object>
                {
                    { "fileName", "test-document.txt" },
                    { "documentId", "doc-001" },
                    { "chunkIndex", 0 },
                    { "chunkText", "This is test content from the stub vector store." }
                }),
            new VectorHit(
                "2",
                0.85f,
                new Dictionary<string, object>
                {
                    { "fileName", "test-document.txt" },
                    { "documentId", "doc-001" },
                    { "chunkIndex", 1 },
                    { "chunkText", "Additional test content for RAG queries." }
                })
        };

        var stubVectorStore = new StubVectorStore(searchResults);
        var stubEmbeddingClient = new StubEmbeddingClient();
        var stubChatClient = new StubChatClient();
        var logger = NullLogger<RagService>.Instance;
        
        var ragService = new RagService(
            stubEmbeddingClient,
            stubChatClient,
            stubVectorStore,
            temperature: 0.0,
            maxTokens: 512,
            logger);

        // Act
        var result = await ragService.AskAsync("What is in the document?", topK: 3);

        // Assert
        result.Should().NotBeNull();
        result.Answer.Should().NotBeNullOrEmpty();
        result.Citations.Should().NotBeNull();
        // Citations should contain the fileName from lowercase metadata
        result.Citations.Should().Contain(c => c.Contains("test-document.txt"));
    }

    [Fact]
    public async Task StubVectorStore_ReturnsLowercaseMetadataKeys()
    {
        // Arrange
        var searchResults = new List<VectorHit>
        {
            new VectorHit(
                "1",
                0.95f,
                new Dictionary<string, object>
                {
                    { "fileName", "test-document.txt" },
                    { "documentId", "doc-001" },
                    { "chunkIndex", 0 },
                    { "chunkText", "This is test content from the stub vector store." }
                })
        };
        var stubVectorStore = new StubVectorStore(searchResults);

        // Act - Use a dummy embedding vector (size doesn't matter for stub)
        var hits = await stubVectorStore.SearchAsync(new float[100], topK: 5);

        // Assert
        hits.Should().NotBeEmpty();
        var metadata = hits.First().Metadata;
        
        // Should contain lowercase keys
        metadata.Should().ContainKey("fileName");
        metadata.Should().ContainKey("documentId");
        metadata.Should().ContainKey("chunkIndex");
        metadata.Should().ContainKey("chunkText");
        
        // Should NOT contain PascalCase keys
        metadata.Keys.Should().NotContain("FileName");
        metadata.Keys.Should().NotContain("DocumentId");
        metadata.Keys.Should().NotContain("ChunkIndex");
        metadata.Keys.Should().NotContain("Content");
    }
}
