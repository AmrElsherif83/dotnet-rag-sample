namespace RAG.Core.Models;

/// <summary>
/// Represents a message in a chat conversation.
/// </summary>
/// <param name="Role">The role of the message sender (system, user, assistant)</param>
/// <param name="Content">The content of the message</param>
public record ChatMessage(string Role, string Content);
