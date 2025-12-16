using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RAG.Core.Abstractions;
using RAG.Core.Services;
using Xunit;

namespace RAG.UnitTests.Services;

public class IngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ValidContent_ReturnsSuccessWithCorrectChunkCount()
    {
        // Arrange
        var mockEmbeddingClient = new Mock<IEmbeddingClient>();
        var mockVectorStore = new Mock<IVectorStore>();

        // Setup embedding client to return fixed embeddings
        var fixedEmbedding = new float[1536];
        mockEmbeddingClient
            .Setup(x => x.GetEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> texts, CancellationToken _) =>
            {
                return texts.Select(_ => fixedEmbedding).ToList();
            });

        // Setup vector store to accept upserts
        mockVectorStore
            .Setup(x => x.UpsertAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<float[]>(),
                It.IsAny<IReadOnlyDictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new IngestionService(
            mockEmbeddingClient.Object, 
            mockVectorStore.Object,
            NullLogger<IngestionService>.Instance);

        // Act - Use content that will create multiple chunks
        var content = string.Join("\n\n", Enumerable.Repeat("This is a paragraph with enough content to be its own chunk when using default settings.", 3));
        var result = await service.IngestAsync("test.txt", content);

        // Assert
        result.Success.Should().BeTrue();
        result.ChunksCreated.Should().BeGreaterThan(0);
        result.ErrorMessage.Should().BeNull();
        result.FileName.Should().Be("test.txt");

        // Verify that GetEmbeddingsAsync was called once
        mockEmbeddingClient.Verify(
            x => x.GetEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify that UpsertAsync was called at least once
        mockVectorStore.Verify(
            x => x.UpsertAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<float[]>(),
                It.IsAny<IReadOnlyDictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task IngestAsync_EmptyContent_ReturnsFailureWithZeroChunks()
    {
        // Arrange
        var mockEmbeddingClient = new Mock<IEmbeddingClient>();
        var mockVectorStore = new Mock<IVectorStore>();

        var service = new IngestionService(
            mockEmbeddingClient.Object, 
            mockVectorStore.Object,
            NullLogger<IngestionService>.Instance);

        // Act
        var result = await service.IngestAsync("test.txt", "");

        // Assert
        result.Success.Should().BeFalse();
        result.ChunksCreated.Should().Be(0);
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage.Should().Contain("empty");

        // Verify that no embeddings were generated
        mockEmbeddingClient.Verify(
            x => x.GetEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify that no upserts were performed
        mockVectorStore.Verify(
            x => x.UpsertAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<float[]>(),
                It.IsAny<IReadOnlyDictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IngestAsync_WhitespaceContent_ReturnsFailureWithZeroChunks()
    {
        // Arrange
        var mockEmbeddingClient = new Mock<IEmbeddingClient>();
        var mockVectorStore = new Mock<IVectorStore>();

        var service = new IngestionService(
            mockEmbeddingClient.Object, 
            mockVectorStore.Object,
            NullLogger<IngestionService>.Instance);

        // Act
        var result = await service.IngestAsync("test.txt", "   \n\n   ");

        // Assert
        result.Success.Should().BeFalse();
        result.ChunksCreated.Should().Be(0);
        result.ErrorMessage.Should().NotBeNull();
    }
}
