using Microsoft.AspNetCore.Mvc;
using RAG.Core.Services;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AskController : ControllerBase
{
    private readonly RagService _ragService;

    public AskController(RagService ragService)
    {
        _ragService = ragService;
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
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required");
        }

        if (request.TopK <= 0)
        {
            return BadRequest("TopK must be greater than 0");
        }

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
