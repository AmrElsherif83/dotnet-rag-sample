namespace RAG.Infrastructure.Configuration;

/// <summary>
/// Configuration options for OpenAI API clients.
/// </summary>
public class OpenAiOptions
{
    /// <summary>
    /// The configuration section name for OpenAI options.
    /// </summary>
    public const string SectionName = "OpenAI";
    
    /// <summary>
    /// OpenAI API key. Can be set via OPENAI_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// Base URL for OpenAI API. Defaults to https://api.openai.com/v1
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    
    /// <summary>
    /// Model to use for embeddings. Defaults to text-embedding-ada-002.
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    
    /// <summary>
    /// Model to use for chat completions. Defaults to gpt-4o-mini.
    /// </summary>
    public string ChatModel { get; set; } = "gpt-4o-mini";
    
    /// <summary>
    /// Sampling temperature for chat completions. Default is 0.0.
    /// </summary>
    public double Temperature { get; set; } = 0.0;

    /// <summary>
    /// Maximum tokens in the chat response. Default is 512.
    /// </summary>
    public int MaxTokens { get; set; } = 512;
}
