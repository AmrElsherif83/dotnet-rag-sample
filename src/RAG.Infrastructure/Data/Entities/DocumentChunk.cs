using Pgvector;

namespace RAG.Infrastructure.Data.Entities;

/// <summary>
/// Represents a chunk of a document with its embedding vector.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Gets or sets the unique identifier for the chunk.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the document identifier for grouping chunks.
    /// </summary>
    public string DocumentId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the chunk index within the document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Gets or sets the text content of the chunk.
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Gets or sets the embedding vector (1536 dimensions).
    /// </summary>
    public Vector Embedding { get; set; } = null!;

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}
