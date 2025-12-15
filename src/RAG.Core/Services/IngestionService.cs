using RAG.Core.Abstractions;
using RAG.Core.Chunking;
using RAG.Core.Models;

namespace RAG.Core.Services;

/// <summary>
/// Service for ingesting and processing documents into the vector store.
/// </summary>
public class IngestionService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionService"/> class.
    /// </summary>
    /// <param name="embeddingClient">The embedding client for generating text embeddings.</param>
    /// <param name="vectorStore">The vector store for persisting embeddings.</param>
    public IngestionService(IEmbeddingClient embeddingClient, IVectorStore vectorStore)
    {
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
    }

    /// <summary>
    /// Ingests a document by chunking the text, generating embeddings, and storing them in the vector store.
    /// </summary>
    /// <param name="fileName">The name of the file being ingested.</param>
    /// <param name="text">The text content to ingest.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IngestResult"/> containing the ingestion outcome.</returns>
    public async Task<IngestResult> IngestAsync(string fileName, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return new IngestResult(fileName, 0, false, "Text content is empty.");
            }

            // Chunk the text using paragraph-aware chunking
            var chunks = TextChunker.ChunkByParagraphs(text);

            if (chunks.Count == 0)
            {
                return new IngestResult(fileName, 0, false, "No chunks were created from the text.");
            }

            // Get embeddings for all chunks
            var embeddings = await _embeddingClient.GetEmbeddingsAsync(chunks, cancellationToken);

            // Upsert each chunk to the vector store
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkId = $"{fileName}_{i}";
                var metadata = new Dictionary<string, object>
                {
                    { "fileName", fileName },
                    { "chunkIndex", i },
                    { "chunkText", chunks[i] }
                };

                await _vectorStore.UpsertAsync(chunkId, embeddings[i], metadata, cancellationToken);
            }

            return new IngestResult(fileName, chunks.Count, true, null);
        }
        catch (Exception ex)
        {
            return new IngestResult(fileName, 0, false, ex.Message);
        }
    }
}
