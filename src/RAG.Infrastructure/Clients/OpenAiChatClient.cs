using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RAG.Core.Abstractions;
using RAG.Core.Models;
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
    /// Sends a chat completion request with multiple messages.
    /// </summary>
    /// <param name="messages">The conversation messages</param>
    /// <param name="temperature">Sampling temperature (0.0 to 2.0)</param>
    /// <param name="maxTokens">Maximum tokens in the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The assistant's response</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public async Task<string> AskAsync(
        IEnumerable<ChatMessage> messages, 
        double temperature = 0.0, 
        int maxTokens = 512, 
        CancellationToken cancellationToken = default)
    {
        if (messages == null || !messages.Any())
        {
            throw new ArgumentException("Messages cannot be null or empty.", nameof(messages));
        }

        var requestBody = new
        {
            model = _options.ChatModel,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            temperature = temperature,
            max_tokens = maxTokens
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(requestBody, jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
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

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseJson, jsonOptions);
        
        if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
        {
            throw new HttpRequestException("OpenAI API returned an invalid response: no choices found.");
        }

        var messageContent = chatResponse.Choices[0].Message?.Content;
        
        if (string.IsNullOrEmpty(messageContent))
        {
            throw new HttpRequestException("OpenAI API returned an invalid response: message content is empty.");
        }

        return messageContent;
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
        public ChatMessageResponse? Message { get; init; }
    }
    
    /// <summary>
    /// Chat message response model.
    /// </summary>
    internal record ChatMessageResponse
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }
}
