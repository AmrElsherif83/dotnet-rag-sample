using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RAG.Core.Abstractions;
using RAG.Infrastructure.Configuration;

namespace RAG.Infrastructure.Clients;

/// <summary>
/// OpenAI implementation of the embedding client.
/// </summary>
public class OpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiEmbeddingClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="options">The OpenAI configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when API key is not configured.</exception>
    public OpenAiEmbeddingClient(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options)
    {
        if (httpClientFactory == null)
        {
            throw new ArgumentNullException(nameof(httpClientFactory));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options.Value;

        // Get API key from options or environment variable
        var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Set it via OpenAI:ApiKey configuration or OPENAI_API_KEY environment variable.");
        }

        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    /// <summary>
    /// Generates an embedding vector for the specified text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A float array representing the embedding vector.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));
        }

        var request = new EmbeddingRequest
        {
            Model = _options.EmbeddingModel,
            Input = text
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/embeddings", request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException("Failed to send request to OpenAI API.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var truncatedBody = errorBody.Length > 500 ? errorBody.Substring(0, 500) : errorBody;
            throw new HttpRequestException(
                $"OpenAI API request failed with status {response.StatusCode}. Response: {truncatedBody}");
        }

        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        
        if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
        {
            throw new HttpRequestException("OpenAI API returned an invalid response: no embedding data found.");
        }

        return embeddingResponse.Data[0].Embedding;
    }

    /// <summary>
    /// Generates embedding vectors for multiple texts in a batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of embedding vectors.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public async Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts == null)
        {
            throw new ArgumentNullException(nameof(texts));
        }

        var textList = texts.ToList();
        
        if (textList.Count == 0)
        {
            throw new ArgumentException("Texts cannot be empty.", nameof(texts));
        }

        var request = new EmbeddingBatchRequest
        {
            Model = _options.EmbeddingModel,
            Input = textList
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/embeddings", request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException("Failed to send request to OpenAI API.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var truncatedBody = errorBody.Length > 500 ? errorBody.Substring(0, 500) : errorBody;
            throw new HttpRequestException(
                $"OpenAI API request failed with status {response.StatusCode}. Response: {truncatedBody}");
        }

        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        
        if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
        {
            throw new HttpRequestException("OpenAI API returned an invalid response: no embedding data found.");
        }

        // Sort by index to maintain order
        var sortedData = embeddingResponse.Data.OrderBy(d => d.Index).ToList();
        return sortedData.Select(d => d.Embedding).ToList();
    }

    /// <summary>
    /// Request model for single text embedding.
    /// </summary>
    internal record EmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("input")]
        public string Input { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request model for batch text embeddings.
    /// </summary>
    internal record EmbeddingBatchRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("input")]
        public List<string> Input { get; init; } = new();
    }

    /// <summary>
    /// Response model for embedding requests.
    /// </summary>
    internal record EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; init; } = new();
    }

    /// <summary>
    /// Embedding data model.
    /// </summary>
    internal record EmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; init; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; init; } = Array.Empty<float>();
    }
}
