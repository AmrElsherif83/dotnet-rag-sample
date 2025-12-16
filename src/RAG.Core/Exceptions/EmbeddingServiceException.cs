namespace RAG.Core.Exceptions;

/// <summary>
/// Exception thrown when the embedding service fails.
/// </summary>
public class EmbeddingServiceException : Exception
{
    public EmbeddingServiceException(string message) : base(message) { }
    
    public EmbeddingServiceException(string message, Exception innerException) 
        : base(message, innerException) { }
}
