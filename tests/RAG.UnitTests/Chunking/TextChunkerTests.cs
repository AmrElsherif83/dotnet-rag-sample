using FluentAssertions;
using RAG.Core.Chunking;
using Xunit;

namespace RAG.UnitTests.Chunking;

public class TextChunkerTests
{
    [Fact]
    public void ChunkByCharacters_LongText_SplitsIntoChunksWithOverlap()
    {
        // Arrange - Create a 2600 character string
        var text = new string('A', 2600);

        // Act
        var chunks = TextChunker.ChunkByCharacters(text, chunkSize: 500, overlap: 50);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks.Count.Should().BeGreaterThan(1);
        
        // With chunk size 500 and overlap 50, we move forward by 450 each time
        // 2600 chars / 450 = ~5.78, so we expect 6 chunks
        chunks.Count.Should().Be(6);
        
        // Each chunk (except possibly the last) should be around 500 chars
        chunks[0].Length.Should().Be(500);
        chunks[1].Length.Should().Be(500);
        
        // Last chunk may be shorter
        chunks[^1].Length.Should().BeLessOrEqualTo(500);
        
        // Verify overlap - end of first chunk should overlap with start of second
        var endOfFirst = chunks[0].Substring(450);  // Last 50 chars
        var startOfSecond = chunks[1].Substring(0, 50);  // First 50 chars
        endOfFirst.Should().Be(startOfSecond);
    }

    [Fact]
    public void ChunkByCharacters_ShortText_ReturnsSingleChunk()
    {
        // Arrange
        var text = "This is a short text.";

        // Act
        var chunks = TextChunker.ChunkByCharacters(text, chunkSize: 500, overlap: 50);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Should().Be(text);
    }

    [Fact]
    public void ChunkByParagraphs_ThreeParagraphs_ReturnsThreeChunks()
    {
        // Arrange - Use smaller max chunk size to force separation
        var text = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";

        // Act - Using small maxChunkSize to force each paragraph into separate chunk
        var chunks = TextChunker.ChunkByParagraphs(text, maxChunkSize: 20);

        // Assert
        chunks.Should().HaveCount(3);
        chunks[0].Should().Be("Paragraph one.");
        chunks[1].Should().Be("Paragraph two.");
        chunks[2].Should().Be("Paragraph three.");
    }

    [Fact]
    public void ChunkByParagraphs_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        var text = "";

        // Act
        var chunks = TextChunker.ChunkByParagraphs(text, maxChunkSize: 1000);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void ChunkByParagraphs_WhitespaceOnly_ReturnsEmptyList()
    {
        // Arrange
        var text = "   \n\n   \n\n   ";

        // Act
        var chunks = TextChunker.ChunkByParagraphs(text, maxChunkSize: 1000);

        // Assert
        chunks.Should().BeEmpty();
    }
}
