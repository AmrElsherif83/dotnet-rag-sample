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
                // Create metadata dictionary for VectorHit with lowercase keys
                var hitMetadata = new Dictionary<string, object>();
                foreach (var kvp in metadata)
                {
                    hitMetadata[kvp.Key] = kvp.Value;
                }
                
                // Ensure lowercase metadata keys are present
                if (!hitMetadata.ContainsKey("fileName"))
                    hitMetadata["fileName"] = metadata.ContainsKey("fileName") ? metadata["fileName"] : "";
                if (!hitMetadata.ContainsKey("documentId"))
                    hitMetadata["documentId"] = docId;
                if (!hitMetadata.ContainsKey("chunkIndex"))
                    hitMetadata["chunkIndex"] = chunkIndex;
                if (!hitMetadata.ContainsKey("chunkText"))
                    hitMetadata["chunkText"] = metadata.ContainsKey("chunkText") ? metadata["chunkText"] : "";
                
                var id = $"{docId}_{chunkIndex}";
                results.Add(new VectorHit(id, 0.9f, hitMetadata));
            }
        }
        
        return Task.FromResult<IReadOnlyList<VectorHit>>(results.Take(topK).ToList());
    }
}
