using RAG.Core.Abstractions;
using RAG.Core.Models;

namespace RAG.UnitTests.Stubs;

/// <summary>
/// Stub implementation of IVectorStore for testing purposes.
/// Returns configurable VectorHit list.
/// </summary>
public class StubVectorStore : IVectorStore
{
    private readonly List<VectorHit> _searchResults;

    public StubVectorStore(List<VectorHit>? searchResults = null)
    {
        _searchResults = searchResults ?? new List<VectorHit>();
    }

    public Task UpsertAsync(string id, float[] embedding, Dictionary<string, object> metadata, CancellationToken cancellationToken = default)
    {
        // No-op for stub
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorHit>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        var results = _searchResults.Take(topK).ToList();
        return Task.FromResult<IReadOnlyList<VectorHit>>(results);
    }
}
