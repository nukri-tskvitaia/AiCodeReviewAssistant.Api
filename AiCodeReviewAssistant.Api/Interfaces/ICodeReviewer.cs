using AiCodeReviewAssistant.Api.Models;

namespace AiCodeReviewAssistant.Api.Interfaces;

public interface ICodeReviewer
{
    ReviewerType ReviewerType { get; }

    Task<ReviewerResult> ReviewAsync(
        CodeReviewRequest request,
        CancellationToken cancellationToken = default);
}
