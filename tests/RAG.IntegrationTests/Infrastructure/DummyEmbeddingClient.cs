using RAG.Core.Abstractions;

namespace RAG.IntegrationTests.Infrastructure;

/// <summary>
/// Dummy implementation of IEmbeddingClient for integration testing.
/// Returns deterministic results.
/// </summary>
public class DummyEmbeddingClient : IEmbeddingClient
{
    private readonly float[] _fixedEmbedding;

    public DummyEmbeddingClient()
    {
        // Create a fixed 1536-dimension embedding vector
        _fixedEmbedding = new float[1536];
        for (int i = 0; i < 1536; i++)
        {
            _fixedEmbedding[i] = 0.001f * i;
        }
    }

    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((float[])_fixedEmbedding.Clone());
    }

    public Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = texts.Select(_ => (float[])_fixedEmbedding.Clone()).ToList();
        return Task.FromResult<IReadOnlyList<float[]>>(embeddings);
    }
}
