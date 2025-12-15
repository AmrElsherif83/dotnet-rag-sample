using RAG.Core.Abstractions;
using RAG.Core.Models;

namespace RAG.Core.Services;

/// <summary>
/// Service for performing Retrieval-Augmented Generation (RAG) queries.
/// </summary>
public class RagService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IChatClient _chatClient;
    private readonly IVectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="RagService"/> class.
    /// </summary>
    /// <param name="embeddingClient">The embedding client for generating query embeddings.</param>
    /// <param name="chatClient">The chat client for generating answers.</param>
    /// <param name="vectorStore">The vector store for searching relevant context.</param>
    public RagService(IEmbeddingClient embeddingClient, IChatClient chatClient, IVectorStore vectorStore)
    {
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
    }

    /// <summary>
    /// Answers a question using Retrieval-Augmented Generation.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <param name="topK">The number of most relevant chunks to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="RagAnswer"/> containing the answer and source citations.</returns>
    public async Task<RagAnswer> AskAsync(string question, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question cannot be null or empty.", nameof(question));
        }

        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), "TopK must be greater than zero.");
        }

        // Embed the question
        var questionEmbedding = await _embeddingClient.GetEmbeddingAsync(question, cancellationToken);

        // Search for similar chunks
        var searchResults = await _vectorStore.SearchAsync(questionEmbedding, topK, cancellationToken);

        // Extract context and citations from search results
        var contextParts = new List<string>();
        var citations = new List<string>();

        for (int i = 0; i < searchResults.Count; i++)
        {
            var hit = searchResults[i];
            
            // Extract chunk text from metadata
            if (hit.Metadata.TryGetValue("chunkText", out var chunkTextObj) && chunkTextObj is string chunkText)
            {
                contextParts.Add($"[Source {i + 1}]: {chunkText}");
                
                // Build citation with file name if available
                var citation = chunkText;
                if (hit.Metadata.TryGetValue("fileName", out var fileNameObj) && fileNameObj is string fileName)
                {
                    citation = $"{fileName}: {chunkText}";
                }
                citations.Add(citation);
            }
        }

        // Build the prompt with context
        var context = string.Join("\n\n", contextParts);
        var prompt = $@"You are a helpful assistant. Answer the question based on the following context. If the context doesn't contain enough information to answer the question, say so.

Context:
{context}

Question: {question}

Answer:";

        // Get the answer from the chat client
        var answer = await _chatClient.AskAsync(prompt, cancellationToken);

        return new RagAnswer(answer, citations);
    }
}
