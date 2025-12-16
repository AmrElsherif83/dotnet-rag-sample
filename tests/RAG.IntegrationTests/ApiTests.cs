using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using RAG.IntegrationTests.Infrastructure;
using Xunit;

namespace RAG.IntegrationTests;

public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CanIngestAndAsk()
    {
        // Arrange - Create file content for ingestion
        using var content = new MultipartFormDataContent();
        var documentText = "This is a sample document for testing the RAG system. " +
                          "It contains information about software development practices. " +
                          "The document covers topics like testing, deployment, and maintenance.";
        var bytes = Encoding.UTF8.GetBytes(documentText);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test-doc.txt");

        // Act - Ingest the document
        var ingestResponse = await _client.PostAsync("/api/ingest", content);

        // Assert - Ingestion succeeded
        ingestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ingestResult = await ingestResponse.Content.ReadFromJsonAsync<IngestResponse>();
        ingestResult.Should().NotBeNull();
        ingestResult!.Success.Should().BeTrue();
        ingestResult.ChunksCreated.Should().BeGreaterThan(0);
        ingestResult.FileName.Should().Be("test-doc.txt");

        // Act - Ask a question about the document
        var askPayload = new { question = "What is this document about?", topK = 3 };
        var askResponse = await _client.PostAsJsonAsync("/api/ask", askPayload);

        // Assert - Question answered successfully
        askResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var askResult = await askResponse.Content.ReadFromJsonAsync<AskResponse>();
        askResult.Should().NotBeNull();
        askResult!.Answer.Should().NotBeNullOrWhiteSpace();
        askResult.Citations.Should().NotBeNull();
    }

    [Fact]
    public async Task Ingest_NoFile_ReturnsBadRequest()
    {
        // Arrange - Empty file (zero length)
        using var content = new MultipartFormDataContent();
        var emptyBytes = Array.Empty<byte>();
        var fileContent = new ByteArrayContent(emptyBytes);
        content.Add(fileContent, "file", "empty.txt");

        // Act
        var response = await _client.PostAsync("/api/ingest", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("No file provided");
    }

    [Fact]
    public async Task Ingest_UnsupportedFileType_ReturnsBadRequest()
    {
        // Arrange - Create a .pdf file (unsupported)
        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("fake pdf content");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "document.pdf");

        // Act
        var response = await _client.PostAsync("/api/ingest", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Unsupported file type");
    }

    [Fact]
    public async Task Ingest_SupportedMarkdownFile_ReturnsOk()
    {
        // Arrange - Create a .md file (supported)
        using var content = new MultipartFormDataContent();
        var markdown = "# Test Document\n\nThis is a markdown file for testing.";
        var bytes = Encoding.UTF8.GetBytes(markdown);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/markdown");
        content.Add(fileContent, "file", "readme.md");

        // Act
        var response = await _client.PostAsync("/api/ingest", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IngestResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.FileName.Should().Be("readme.md");
    }

    [Fact]
    public async Task Ask_EmptyQuestion_ReturnsBadRequest()
    {
        // Arrange
        var payload = new { question = "", topK = 5 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ask", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Question is required");
    }

    [Fact]
    public async Task Ask_WhitespaceQuestion_ReturnsBadRequest()
    {
        // Arrange
        var payload = new { question = "   ", topK = 5 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ask", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Question is required");
    }

    [Fact]
    public async Task Ask_ZeroTopK_ReturnsBadRequest()
    {
        // Arrange
        var payload = new { question = "What is testing?", topK = 0 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ask", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("TopK must be greater than 0");
    }

    [Fact]
    public async Task Ask_NegativeTopK_ReturnsBadRequest()
    {
        // Arrange
        var payload = new { question = "What is testing?", topK = -1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ask", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("TopK must be greater than 0");
    }

    [Fact]
    public async Task Ask_ValidRequest_ReturnsOk()
    {
        // Arrange - First ingest a document
        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("Software testing is important for quality assurance.");
        var fileContent = new ByteArrayContent(bytes);
        content.Add(fileContent, "file", "testing-doc.txt");
        await _client.PostAsync("/api/ingest", content);

        // Act - Ask a question
        var payload = new { question = "What is important for quality?", topK = 3 };
        var response = await _client.PostAsJsonAsync("/api/ask", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AskResponse>();
        result.Should().NotBeNull();
        result!.Answer.Should().NotBeNullOrWhiteSpace();
        result.Citations.Should().NotBeNull();
    }

    [Fact]
    public async Task Ask_NullQuestion_ReturnsBadRequest()
    {
        // Arrange - Send request with null question
        var payload = new { question = (string?)null, topK = 5 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ask", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ingest_UnsupportedExtension_Exe_ReturnsBadRequest()
    {
        // Arrange - Create a .exe file (definitely unsupported)
        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("fake executable content");
        var fileContent = new ByteArrayContent(bytes);
        content.Add(fileContent, "file", "malware.exe");

        // Act
        var response = await _client.PostAsync("/api/ingest", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Unsupported file type");
    }

    [Fact]
    public async Task Ingest_UnsupportedExtension_Docx_ReturnsBadRequest()
    {
        // Arrange - Create a .docx file (unsupported for now)
        using var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("fake docx content");
        var fileContent = new ByteArrayContent(bytes);
        content.Add(fileContent, "file", "document.docx");

        // Act
        var response = await _client.PostAsync("/api/ingest", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Unsupported file type");
    }
}

// Response DTOs for deserialization
internal class IngestResponse
{
    public string FileName { get; set; } = string.Empty;
    public int ChunksCreated { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

internal class AskResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<string> Citations { get; set; } = new();
}
