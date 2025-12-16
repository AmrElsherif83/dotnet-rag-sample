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
    private readonly double _temperature;
    private readonly int _maxTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="RagService"/> class.
    /// </summary>
    /// <param name="embeddingClient">The embedding client for generating query embeddings.</param>
    /// <param name="chatClient">The chat client for generating answers.</param>
    /// <param name="vectorStore">The vector store for searching relevant context.</param>
    /// <param name="temperature">Sampling temperature for chat completions.</param>
    /// <param name="maxTokens">Maximum tokens in chat response.</param>
    public RagService(
        IEmbeddingClient embeddingClient, 
        IChatClient chatClient, 
        IVectorStore vectorStore,
        double temperature = 0.0,
        int maxTokens = 512)
    {
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _temperature = temperature;
        _maxTokens = maxTokens;
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
                // Build context parts with source numbering
                var fileName = "unknown";
                if (hit.Metadata.TryGetValue("fileName", out var fileNameObj) && fileNameObj is string fn)
                {
                    fileName = fn;
                }
                
                contextParts.Add($"[{i + 1}] (Source: {fileName})\n{chunkText}");
                
                // Build citation with file name and preview
                var preview = chunkText.Length > 100 ? chunkText.Substring(0, 100) + "..." : chunkText;
                citations.Add($"{fileName}: {preview}");
            }
        }

        // Build context from hits
        var context = string.Join("\n\n", contextParts);

        // Create messages with system and user roles
        var messages = new[]
        {
            new ChatMessage("system", "You are a helpful assistant. Answer ONLY using the provided context. If the answer is not in the context, respond with 'I don't know.'"),
            new ChatMessage("user", $"Context:\n{context}\n\nQuestion: {question}")
        };

        // Call chat client with temperature and maxTokens from options
        var answer = await _chatClient.AskAsync(
            messages, 
            _temperature, 
            _maxTokens, 
            cancellationToken);

        return new RagAnswer(answer, citations);
    }
}
