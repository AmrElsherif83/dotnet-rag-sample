using Microsoft.AspNetCore.Mvc;
using RAG.Core.Services;

namespace RAG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestController : ControllerBase
{
    private readonly IngestionService _ingestionService;

    public IngestController(IngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    /// <summary>
    /// Ingest a text or markdown file for RAG processing.
    /// </summary>
    /// <param name="file">A .txt or .md file to ingest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ingestion result with chunk count</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestFile(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".txt" && extension != ".md")
        {
            return BadRequest("Unsupported file type. Only .txt and .md files are allowed.");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var text = await reader.ReadToEndAsync(cancellationToken);

        var result = await _ingestionService.IngestAsync(file.FileName, text, cancellationToken);

        return Ok(result);
    }
}
