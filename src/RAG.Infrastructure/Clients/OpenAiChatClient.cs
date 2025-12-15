using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RAG.Core.Abstractions;
using RAG.Infrastructure.Configuration;

namespace RAG.Infrastructure.Clients;

/// <summary>
/// OpenAI implementation of the chat client.
/// </summary>
public class OpenAiChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiChatClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="options">The OpenAI configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when API key is not configured.</exception>
    public OpenAiChatClient(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options)
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
    /// Sends a prompt to the chat model and returns the response.
    /// </summary>
    /// <param name="prompt">The prompt to send to the model.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The model's response as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        }

        var request = new ChatRequest
        {
            Model = _options.ChatModel,
            Messages = new List<ChatMessage>
            {
                new ChatMessage
                {
                    Role = "user",
                    Content = prompt
                }
            },
            Temperature = 0.7
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/chat/completions", request, cancellationToken);
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

        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
        
        if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
        {
            throw new HttpRequestException("OpenAI API returned an invalid response: no choices found.");
        }

        var content = chatResponse.Choices[0].Message?.Content;
        
        if (string.IsNullOrEmpty(content))
        {
            throw new HttpRequestException("OpenAI API returned an invalid response: message content is empty.");
        }

        return content;
    }

    /// <summary>
    /// Request model for chat completions.
    /// </summary>
    internal record ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; init; } = new();

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }
    }

    /// <summary>
    /// Chat message model.
    /// </summary>
    internal record ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response model for chat completions.
    /// </summary>
    internal record ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice> Choices { get; init; } = new();
    }

    /// <summary>
    /// Chat choice model.
    /// </summary>
    internal record ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; init; }
    }
}
