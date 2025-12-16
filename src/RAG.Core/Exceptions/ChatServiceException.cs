namespace RAG.Core.Exceptions;

/// <summary>
/// Exception thrown when the chat service fails.
/// </summary>
public class ChatServiceException : Exception
{
    public ChatServiceException(string message) : base(message) { }
    
    public ChatServiceException(string message, Exception innerException) 
        : base(message, innerException) { }
}
