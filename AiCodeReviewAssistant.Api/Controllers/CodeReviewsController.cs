using AiCodeReviewAssistant.Api.Models;
using AiCodeReviewAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCodeReviewAssistant.Api.Controllers;

[ApiController]
[Route("api/code-reviews")]
public sealed class CodeReviewsController(CodeReviewOrchestrator orchestrator) : ControllerBase
{
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(CodeReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CodeReviewResponse>> Analyze(
        [FromBody] CodeReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.AnalyzeAsync(request, cancellationToken);

        return Ok(result);
    }
}