using AiCodeReviewAssistant.Api.Interfaces;
using AiCodeReviewAssistant.Api.Models;

namespace AiCodeReviewAssistant.Api.Services.Reviewers;

public sealed class CleanCodeReviewer(ClaudeClient claudeClient) : ICodeReviewer
{
    public ReviewerType ReviewerType => ReviewerType.CleanCode;

    public Task<ReviewerResult> ReviewAsync(
        CodeReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewerRequest = new CodeReviewerRequest
        {
            Language = request.Language,
            Code = request.Code,
            ReviewerType = ReviewerType.CleanCode
        };

        return claudeClient.ReviewCodeAsync(reviewerRequest, cancellationToken);
    }
}