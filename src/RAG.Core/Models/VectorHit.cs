namespace RAG.Core.Models;

/// <summary>
/// Represents a vector search result with similarity score and metadata.
/// </summary>
/// <param name="Id">The unique identifier of the vector.</param>
/// <param name="Score">The similarity score (higher is more similar).</param>
/// <param name="Metadata">Additional metadata associated with the vector.</param>
public record VectorHit(
    string Id,
    float Score,
    Dictionary<string, object> Metadata
);
