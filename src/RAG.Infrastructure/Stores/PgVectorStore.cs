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
    /// <param name="id">The unique identifier for the vector (format: "documentId_chunkIndex").</param>
    /// <param name="embedding">The embedding vector.</param>
    /// <param name="metadata">Additional metadata to store with the vector.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task UpsertAsync(string id, float[] embedding, Dictionary<string, object> metadata, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));
        }

        if (embedding == null || embedding.Length == 0)
        {
            throw new ArgumentException("Embedding cannot be null or empty.", nameof(embedding));
        }

        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        // Extract document ID from the id (format: "fileName_chunkIndex")
        var lastUnderscoreIndex = id.LastIndexOf('_');
        var documentId = lastUnderscoreIndex > 0 ? id.Substring(0, lastUnderscoreIndex) : id;

        // Extract metadata
        var fileName = metadata.TryGetValue("fileName", out var fileNameObj) ? fileNameObj.ToString() : documentId;
        var chunkIndex = metadata.TryGetValue("chunkIndex", out var chunkIndexObj) ? Convert.ToInt32(chunkIndexObj) : 0;
        var chunkText = metadata.TryGetValue("chunkText", out var chunkTextObj) ? chunkTextObj.ToString() : string.Empty;

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = documentId;
        }

        if (string.IsNullOrEmpty(chunkText))
        {
            throw new ArgumentException("Metadata must contain 'chunkText'.", nameof(metadata));
        }

        // Check if this is the first chunk (chunkIndex == 0)
        // If so, delete all existing chunks for this document to handle re-ingestion
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

        // Create new DocumentChunk entity
        var documentChunk = new DocumentChunk
        {
            DocumentId = documentId,
            FileName = fileName,
            ChunkIndex = chunkIndex,
            Content = chunkText,
            Embedding = new Vector(embedding),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.DocumentChunks.Add(documentChunk);
        
        // Save all changes in a single transaction
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
                { "FileName", r.FileName },
                { "DocumentId", r.DocumentId },
                { "Content", r.Content },
                { "ChunkIndex", r.ChunkIndex }
            }
        )).ToList();

        return vectorHits;
    }
}
