using Microsoft.Extensions.Logging;
using RAG.Core.Abstractions;
using RAG.Core.Chunking;
using RAG.Core.Exceptions;
using RAG.Core.Models;

namespace RAG.Core.Services;

/// <summary>
/// Service for ingesting and processing documents into the vector store.
/// </summary>
public class IngestionService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<IngestionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionService"/> class.
    /// </summary>
    /// <param name="embeddingClient">The embedding client for generating text embeddings.</param>
    /// <param name="vectorStore">The vector store for persisting embeddings.</param>
    /// <param name="logger">The logger for structured logging.</param>
    public IngestionService(
        IEmbeddingClient embeddingClient, 
        IVectorStore vectorStore,
        ILogger<IngestionService> logger)
    {
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        _logger.LogInformation("Starting ingestion of file: {FileName}", fileName);
        
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
            _logger.LogDebug("Created {ChunkCount} chunks from {FileName}", chunks.Count, fileName);

            if (chunks.Count == 0)
            {
                return new IngestResult(fileName, 0, false, "No chunks were created from the text.");
            }

            // Get embeddings for all chunks
            IReadOnlyList<float[]> embeddings;
            try
            {
                embeddings = await _embeddingClient.GetEmbeddingsAsync(chunks, cancellationToken);
                _logger.LogInformation("Generated {EmbeddingCount} embeddings for {FileName}", embeddings.Count, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for {FileName}", fileName);
                throw new EmbeddingServiceException($"Failed to generate embeddings for {fileName}", ex);
            }

            // Use fileName as the document ID
            var documentId = fileName;

            // Upsert each chunk to the vector store
            for (int i = 0; i < chunks.Count; i++)
            {
                var metadata = new Dictionary<string, object>
                {
                    { "fileName", fileName },
                    { "chunkIndex", i },
                    { "chunkText", chunks[i] }
                };

                await _vectorStore.UpsertAsync(documentId, i, embeddings[i], metadata, cancellationToken);
            }

            _logger.LogInformation("Successfully ingested {FileName} with {ChunkCount} chunks", fileName, chunks.Count);
            return new IngestResult(fileName, chunks.Count, true, null);
        }
        catch (EmbeddingServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during ingestion of {FileName}", fileName);
            return new IngestResult(fileName, 0, false, ex.Message);
        }
    }
}
