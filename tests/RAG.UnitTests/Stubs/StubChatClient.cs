using RAG.Core.Abstractions;
using RAG.Core.Models;

namespace RAG.UnitTests.Stubs;

/// <summary>
/// Stub implementation of IChatClient for testing purposes.
/// Echoes back the prompt for verification.
/// </summary>
public class StubChatClient : IChatClient
{
    public Task<string> AskAsync(
        IEnumerable<ChatMessage> messages, 
        double temperature = 0.0, 
        int maxTokens = 512, 
        CancellationToken cancellationToken = default)
    {
        var userMessage = messages.LastOrDefault(m => m.Role == "user");
        return Task.FromResult($"Answer based on: {userMessage?.Content ?? "no context"}");
    }
}
