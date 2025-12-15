namespace RAG.Core.Abstractions;

/// <summary>
/// Defines methods for generating text embeddings.
/// </summary>
public interface IEmbeddingClient
{
    /// <summary>
    /// Generates an embedding vector for the specified text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A float array representing the embedding vector.</returns>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in a batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of embedding vectors.</returns>
    Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
