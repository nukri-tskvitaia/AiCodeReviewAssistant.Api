using AiCodeReviewAssistant.Api.Interfaces;
using AiCodeReviewAssistant.Api.Models;
using AiCodeReviewAssistant.Api.Validation;

namespace AiCodeReviewAssistant.Api.Services;

public sealed class CodeReviewOrchestrator(IEnumerable<ICodeReviewer> reviewers,
    ClaudeClient claudeClient)
{
    public async Task<CodeReviewResponse> AnalyzeAsync(
            CodeReviewRequest request,
            CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        var reviewerTasks = reviewers
            .Select(reviewer => RunReviewerSafelyAsync(reviewer, request, timeoutCts.Token))
            .ToList();

        var reviewerResults = await Task.WhenAll(reviewerTasks);

        var successfulResults = reviewerResults
            .Where(result => result is not null)
            .Cast<ReviewerResult>()
            .ToList();

        if (successfulResults.Count == 0)
            throw new InvalidOperationException("All code reviewers failed or timed out.");

        var response = await claudeClient.AggregateReviewAsync(
            successfulResults,
            cancellationToken);

        if (!CodeReviewResponseValidator.IsValid(response, out var validationError))
            throw new InvalidOperationException($"Invalid code review response: {validationError}");

        return response;
    }

    private static async Task<ReviewerResult?> RunReviewerSafelyAsync(
        ICodeReviewer reviewer,
        CodeReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await reviewer.ReviewAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new ReviewerResult
            {
                Name = GetReviewerName(reviewer.ReviewerType),
                Score = 1,
                Findings =
                [
                    "Reviewer timed out before completing analysis."
                ]
            };
        }
        catch (Exception ex)
        {
            return new ReviewerResult
            {
                Name = GetReviewerName(reviewer.ReviewerType),
                Score = 1,
                Findings =
                [
                    $"Reviewer failed during analysis: {ex.Message}"
                ]
            };
        }
    }

    private static string GetReviewerName(ReviewerType reviewerType)
    {
        return reviewerType switch
        {
            ReviewerType.Security => "Security Reviewer",
            ReviewerType.Performance => "Performance Reviewer",
            ReviewerType.CleanCode => "Clean Code Reviewer",
            _ => "Unknown Reviewer"
        };
    }
}