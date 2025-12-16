using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using RAG.IntegrationTests.Infrastructure;
using Xunit;

namespace RAG.IntegrationTests;

/// <summary>
/// Tests for API key authentication middleware.
/// These tests use a separate factory that enables API key auth.
/// </summary>
[Collection("Auth Tests")]
public class ApiKeyAuthTests : IClassFixture<AuthEnabledWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string ValidApiKey = "test-api-key-for-integration-tests";
    private const string InvalidApiKey = "wrong-api-key";

    public ApiKeyAuthTests(AuthEnabledWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Request_WithoutApiKey_Returns401Unauthorized()
    {
        // Arrange
        var payload = new { question = "Test question", topK = 3 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ask", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("API Key is missing");
    }

    [Fact]
    public async Task Request_WithInvalidApiKey_Returns401Unauthorized()
    {
        // Arrange
        var payload = new { question = "Test question", topK = 3 };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/ask");
        request.Headers.Add("X-API-KEY", InvalidApiKey);
        request.Content = JsonContent.Create(payload);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid API Key");
    }

    [Fact]
    public async Task Request_WithValidApiKey_Succeeds()
    {
        // Arrange - First ingest a document
        using var ingestContent = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("Test document content");
        ingestContent.Add(new ByteArrayContent(bytes), "file", "test.txt");
        
        using var ingestRequest = new HttpRequestMessage(HttpMethod.Post, "/api/ingest");
        ingestRequest.Headers.Add("X-API-KEY", ValidApiKey);
        ingestRequest.Content = ingestContent;
        
        await _client.SendAsync(ingestRequest);

        // Act - Ask with valid API key
        using var askRequest = new HttpRequestMessage(HttpMethod.Post, "/api/ask");
        askRequest.Headers.Add("X-API-KEY", ValidApiKey);
        askRequest.Content = JsonContent.Create(new { question = "What is this?", topK = 3 });
        
        var response = await _client.SendAsync(askRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerEndpoint_WithoutApiKey_Succeeds()
    {
        // Arrange & Act - Swagger should be accessible without API key
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert - Should not be 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
