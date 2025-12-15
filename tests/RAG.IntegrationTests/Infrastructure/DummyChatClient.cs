using RAG.Core.Abstractions;

namespace RAG.IntegrationTests.Infrastructure;

/// <summary>
/// Dummy implementation of IChatClient for integration testing.
/// Returns deterministic results.
/// </summary>
public class DummyChatClient : IChatClient
{
    public Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Return a deterministic answer based on the prompt
        return Task.FromResult("This is a test answer based on the provided context.");
    }
}
