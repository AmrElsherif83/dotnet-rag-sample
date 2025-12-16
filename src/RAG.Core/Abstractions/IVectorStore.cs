using RAG.Core.Models;

namespace RAG.Core.Abstractions;

/// <summary>
/// Defines methods for storing and searching vector embeddings.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Inserts or updates a vector embedding with associated metadata.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="chunkIndex">The chunk index within the document.</param>
    /// <param name="embedding">The embedding vector.</param>
    /// <param name="metadata">Additional metadata to store with the vector.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task UpsertAsync(string documentId, int chunkIndex, float[] embedding, IReadOnlyDictionary<string, object> metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for the most similar vectors to the query embedding.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="topK">The number of top results to return.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of search results ordered by similarity score.</returns>
    Task<IReadOnlyList<VectorHit>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
}
