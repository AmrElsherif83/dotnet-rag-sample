namespace RAG.Core.Abstractions;

/// <summary>
/// Defines methods for interacting with a chat/completion model.
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Sends a prompt to the chat model and returns the response.
    /// </summary>
    /// <param name="prompt">The prompt to send to the model.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The model's response as a string.</returns>
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default);
}
