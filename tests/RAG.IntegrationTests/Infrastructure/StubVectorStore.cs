using System.Collections.Concurrent;
using RAG.Core.Abstractions;
using RAG.Core.Models;

namespace RAG.IntegrationTests.Infrastructure;

/// <summary>
/// Stub implementation of IVectorStore for integration testing.
/// Stores data in memory without requiring pgvector.
/// </summary>
public class StubVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, List<(int chunkIndex, float[] embedding, IReadOnlyDictionary<string, object> metadata)>> _store = new();

    public Task UpsertAsync(
        string documentId, 
        int chunkIndex, 
        float[] embedding, 
        IReadOnlyDictionary<string, object> metadata, 
        CancellationToken cancellationToken = default)
    {
        _store.AddOrUpdate(
            documentId,
            _ => new List<(int, float[], IReadOnlyDictionary<string, object>)> { (chunkIndex, embedding, metadata) },
            (_, existingList) =>
            {
                existingList.Add((chunkIndex, embedding, metadata));
                return existingList;
            });
        
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorHit>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        var results = new List<VectorHit>();
        
        // Return stored chunks as search results (simple in-memory search)
        foreach (var (docId, chunks) in _store)
        {
            foreach (var (chunkIndex, embedding, metadata) in chunks)
            {
                // Create metadata dictionary for VectorHit
                var hitMetadata = new Dictionary<string, object>();
                foreach (var kvp in metadata)
                {
                    hitMetadata[kvp.Key] = kvp.Value;
                }
                
                // Add required metadata if not present
                if (!hitMetadata.ContainsKey("FileName"))
                    hitMetadata["FileName"] = metadata.ContainsKey("fileName") ? metadata["fileName"] : "";
                if (!hitMetadata.ContainsKey("DocumentId"))
                    hitMetadata["DocumentId"] = docId;
                if (!hitMetadata.ContainsKey("ChunkIndex"))
                    hitMetadata["ChunkIndex"] = chunkIndex;
                if (!hitMetadata.ContainsKey("Content"))
                    hitMetadata["Content"] = metadata.ContainsKey("chunkText") ? metadata["chunkText"] : "";
                
                var id = $"{docId}_{chunkIndex}";
                results.Add(new VectorHit(id, 0.9f, hitMetadata));
            }
        }
        
        return Task.FromResult<IReadOnlyList<VectorHit>>(results.Take(topK).ToList());
    }
}
