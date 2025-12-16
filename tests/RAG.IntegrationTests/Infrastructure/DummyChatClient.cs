using RAG.Core.Abstractions;
using RAG.Core.Models;

namespace RAG.IntegrationTests.Infrastructure;

/// <summary>
/// Dummy implementation of IChatClient for integration testing.
/// Returns deterministic results.
/// </summary>
public class DummyChatClient : IChatClient
{
    public Task<string> AskAsync(
        IEnumerable<ChatMessage> messages, 
        double temperature = 0.0, 
        int maxTokens = 512, 
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult("This is a test answer based on the provided context.");
    }
}
