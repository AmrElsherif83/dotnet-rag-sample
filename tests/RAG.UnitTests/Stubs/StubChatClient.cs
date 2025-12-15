using RAG.Core.Abstractions;

namespace RAG.UnitTests.Stubs;

/// <summary>
/// Stub implementation of IChatClient for testing purposes.
/// Echoes back the prompt for verification.
/// </summary>
public class StubChatClient : IChatClient
{
    public Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Echo back the prompt so tests can verify what was sent
        return Task.FromResult($"Echo: {prompt}");
    }
}
