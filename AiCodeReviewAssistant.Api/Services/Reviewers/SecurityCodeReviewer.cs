using AiCodeReviewAssistant.Api.Interfaces;
using AiCodeReviewAssistant.Api.Models;

namespace AiCodeReviewAssistant.Api.Services.Reviewers;

public sealed class SecurityCodeReviewer(ClaudeClient claudeClient) : ICodeReviewer
{
    public ReviewerType ReviewerType => ReviewerType.Security;

    public Task<ReviewerResult> ReviewAsync(
        CodeReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewerRequest = new CodeReviewerRequest
        {
            Language = request.Language,
            Code = request.Code,
            ReviewerType = ReviewerType.Security
        };

        return claudeClient.ReviewCodeAsync(reviewerRequest, cancellationToken);
    }
}