using RAG.Core.Models;

namespace RAG.Core.Abstractions;

/// <summary>
/// Interface for chat completion operations.
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Sends a chat completion request with multiple messages.
    /// </summary>
    /// <param name="messages">The conversation messages</param>
    /// <param name="temperature">Sampling temperature (0.0 to 2.0)</param>
    /// <param name="maxTokens">Maximum tokens in the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The assistant's response</returns>
    Task<string> AskAsync(
        IEnumerable<ChatMessage> messages, 
        double temperature = 0.0, 
        int maxTokens = 512, 
        CancellationToken cancellationToken = default);
}
