using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RAG.Core.Abstractions;
using RAG.Core.Models;
using RAG.Infrastructure.Data;
using RAG.Infrastructure.Data.Entities;

namespace RAG.Infrastructure.Stores;

/// <summary>
/// PostgreSQL with pgvector implementation of the vector store.
/// </summary>
public class PgVectorStore : IVectorStore
{
    private readonly RagDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgVectorStore"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public PgVectorStore(RagDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Inserts or updates a vector embedding with associated metadata.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="chunkIndex">The chunk index within the document.</param>
    /// <param name="embedding">The embedding vector.</param>
    /// <param name="metadata">Additional metadata to store with the vector.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task UpsertAsync(
        string documentId, 
        int chunkIndex, 
        float[] embedding, 
        IReadOnlyDictionary<string, object> metadata, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("Document ID cannot be null or empty.", nameof(documentId));
        }

        if (embedding == null || embedding.Length == 0)
        {
            throw new ArgumentException("Embedding cannot be null or empty.", nameof(embedding));
        }

        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        // Extract metadata values
        var fileName = metadata.TryGetValue("fileName", out var fn) ? fn?.ToString() ?? documentId : documentId;
        var content = metadata.TryGetValue("chunkText", out var ct) ? ct?.ToString() ?? string.Empty : string.Empty;

        if (string.IsNullOrEmpty(content))
        {
            throw new ArgumentException("Metadata must contain 'chunkText'.", nameof(metadata));
        }

        // Delete existing chunks for this document on first chunk (chunkIndex == 0)
        if (chunkIndex == 0)
        {
            var existingChunks = await _dbContext.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            if (existingChunks.Any())
            {
                _dbContext.DocumentChunks.RemoveRange(existingChunks);
            }
        }

        // Create new chunk - Id is auto-generated
        var chunk = new DocumentChunk
        {
            DocumentId = documentId,
            FileName = fileName,
            ChunkIndex = chunkIndex,
            Content = content,
            Embedding = new Vector(embedding),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.DocumentChunks.Add(chunk);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Searches for the most similar vectors to the query embedding.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="topK">The number of top results to return.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of search results ordered by similarity score.</returns>
    public async Task<IReadOnlyList<VectorHit>> SearchAsync(
        float[] queryEmbedding, 
        int topK, 
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(queryEmbedding, topK, null, null, cancellationToken);
    }

    /// <summary>
    /// Searches for the most similar vectors to the query embedding with optional filters.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="topK">The number of top results to return.</param>
    /// <param name="fileNameFilter">Optional filter by file name.</param>
    /// <param name="documentIdFilter">Optional filter by document ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of search results ordered by similarity score.</returns>
    public async Task<IReadOnlyList<VectorHit>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        string? fileNameFilter = null,
        string? documentIdFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            throw new ArgumentException("Query embedding cannot be null or empty.", nameof(queryEmbedding));
        }

        if (topK <= 0)
        {
            throw new ArgumentException("TopK must be greater than zero.", nameof(topK));
        }

        var queryVector = new Vector(queryEmbedding);

        // Use LINQ query builder to avoid SQL injection
        var query = _dbContext.DocumentChunks.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(fileNameFilter))
        {
            query = query.Where(c => c.FileName == fileNameFilter);
        }
        if (!string.IsNullOrWhiteSpace(documentIdFilter))
        {
            query = query.Where(c => c.DocumentId == documentIdFilter);
        }

        // Use EF Core vector distance operation and select required fields
        var results = await query
            .Select(c => new
            {
                c.Id,
                c.DocumentId,
                c.FileName,
                c.ChunkIndex,
                c.Content,
                Distance = c.Embedding.CosineDistance(queryVector)
            })
            .OrderBy(c => c.Distance)
            .Take(topK)
            .ToListAsync(cancellationToken);

        // Convert distance to similarity score (1 - distance for cosine)
        var vectorHits = results.Select(r => new VectorHit(
            Id: r.Id.ToString(),
            Score: 1.0f - (float)r.Distance,
            Metadata: new Dictionary<string, object>
            {
                { "fileName", r.FileName },
                { "documentId", r.DocumentId },
                { "chunkText", r.Content },
                { "chunkIndex", r.ChunkIndex }
            }
        )).ToList();

        return vectorHits;
    }
}
