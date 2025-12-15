using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RAG.IntegrationTests.Infrastructure;
using Xunit;

namespace RAG.IntegrationTests;

/// <summary>
/// Integration tests for the RAG API.
/// </summary>
public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(Skip = "API endpoints not yet implemented")]
    public async Task CanIngestAndAsk()
    {
        // Arrange
        var ingestRequest = new
        {
            fileName = "test-doc.txt",
            content = "This is a sample document for testing. It contains important information about the RAG system."
        };

        // Act 1: Ingest document
        var ingestResponse = await _client.PostAsJsonAsync("/api/ingest", ingestRequest);

        // Assert 1: Successful ingestion
        ingestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ingestResult = await ingestResponse.Content.ReadFromJsonAsync<IngestResponse>();
        ingestResult.Should().NotBeNull();
        ingestResult!.ChunksCreated.Should().BeGreaterThan(0);
        ingestResult.Success.Should().BeTrue();

        // Act 2: Ask question
        var askRequest = new
        {
            question = "What is this document about?"
        };
        var askResponse = await _client.PostAsJsonAsync("/api/ask", askRequest);

        // Assert 2: Successful answer
        askResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskResponse>();
        askResult.Should().NotBeNull();
        askResult!.Answer.Should().NotBeNullOrEmpty();
        askResult.Citations.Should().NotBeNull();
    }

    [Fact(Skip = "API endpoints not yet implemented")]
    public async Task Ingest_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange - Empty request
        var ingestRequest = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", ingestRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "API endpoints not yet implemented")]
    public async Task Ingest_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var ingestRequest = new
        {
            fileName = "test.txt",
            content = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingest", ingestRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Response models for deserialization
    private class IngestResponse
    {
        public string FileName { get; set; } = string.Empty;
        public int ChunksCreated { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class AskResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<string> Citations { get; set; } = new();
    }
}
