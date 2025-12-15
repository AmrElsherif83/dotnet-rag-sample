namespace RAG.Core.Models;

/// <summary>
/// Represents the result of a document ingestion operation.
/// </summary>
/// <param name="FileName">The name of the ingested file.</param>
/// <param name="ChunksCreated">The number of text chunks created from the document.</param>
/// <param name="Success">Indicates whether the ingestion was successful.</param>
/// <param name="ErrorMessage">Error message if the ingestion failed, null otherwise.</param>
public record IngestResult(
    string FileName,
    int ChunksCreated,
    bool Success,
    string? ErrorMessage
);
