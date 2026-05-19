using AiCodeReviewAssistant.Api.Models;
using AiCodeReviewAssistant.Api.Validation;

namespace AiCodeReviewAssistant.Api.Services;

public sealed class CodeReviewOrchestrator(ClaudeClient claudeClient)
{
    public async Task<CodeReviewResponse> AnalyzeAsync(
        CodeReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await claudeClient.AnalyzeCodeAsync(request, cancellationToken);

        if (!CodeReviewResponseValidator.IsValid(result, out var validationError))
            throw new InvalidOperationException($"Claude returned invalid code review response: {validationError}");

        return result;
    }
}