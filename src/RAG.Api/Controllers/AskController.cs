using Microsoft.AspNetCore.Mvc;
using RAG.Core.Exceptions;
using RAG.Core.Services;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AskController : ControllerBase
{
    private readonly RagService _ragService;
    private readonly ILogger<AskController> _logger;

    public AskController(RagService ragService, ILogger<AskController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question against the ingested documents.
    /// </summary>
    /// <param name="request">The question request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer with citations</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            _logger.LogWarning("Ask request received with empty question");
            return BadRequest("Question is required");
        }

        if (request.TopK <= 0)
        {
            return BadRequest("TopK must be greater than 0");
        }

        try
        {
            var result = await _ragService.AskAsync(
                request.Question, 
                request.TopK, 
                cancellationToken);

            return Ok(new 
            { 
                answer = result.Answer, 
                citations = result.Citations 
            });
        }
        catch (EmbeddingServiceException ex)
        {
            _logger.LogError(ex, "Embedding service failure during ask");
            return StatusCode(StatusCodes.Status502BadGateway, new 
            { 
                error = "Embedding service unavailable", 
                message = ex.Message 
            });
        }
        catch (ChatServiceException ex)
        {
            _logger.LogError(ex, "Chat service failure during ask");
            return StatusCode(StatusCodes.Status502BadGateway, new 
            { 
                error = "Chat service unavailable", 
                message = ex.Message 
            });
        }
    }
}

/// <summary>
/// Request model for asking questions.
/// </summary>
public class AskRequest
{
    /// <summary>
    /// The question to ask.
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Number of top similar chunks to retrieve. Default is 5.
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Optional filter by file name.
    /// </summary>
    public string? FileNameFilter { get; set; }

    /// <summary>
    /// Optional filter by document ID.
    /// </summary>
    public string? DocumentIdFilter { get; set; }
}
