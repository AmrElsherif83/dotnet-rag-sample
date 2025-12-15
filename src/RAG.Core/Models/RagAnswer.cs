namespace RAG.Core.Models;

/// <summary>
/// Represents the response from a RAG query including the answer and source citations.
/// </summary>
/// <param name="Answer">The generated answer to the question.</param>
/// <param name="Citations">Source references or snippets used to generate the answer.</param>
public record RagAnswer(
    string Answer,
    IReadOnlyList<string> Citations
);
