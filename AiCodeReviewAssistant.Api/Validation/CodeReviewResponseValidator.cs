using AiCodeReviewAssistant.Api.Models;

namespace AiCodeReviewAssistant.Api.Validation;

public static class CodeReviewResponseValidator
{
    private static readonly HashSet<string> ExpectedReviewers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Security Reviewer",
        "Performance Reviewer",
        "Clean Code Reviewer"
    };

    public static bool IsValid(CodeReviewResponse response, out string error)
    {
        if (string.IsNullOrWhiteSpace(response.Summary))
        {
            error = "Summary is required.";
            return false;
        }

        if (response.Reviewers.Count == 0)
        {
            error = "At least one reviewer is required.";
            return false;
        }

        foreach (var reviewer in response.Reviewers)
        {
            if (string.IsNullOrWhiteSpace(reviewer.Name))
            {
                error = "Reviewer name is required.";
                return false;
            }

            if (!ExpectedReviewers.Contains(reviewer.Name))
            {
                error = $"Unexpected reviewer name: {reviewer.Name}.";
                return false;
            }

            if (reviewer.Score is < 1 or > 10)
            {
                error = $"Reviewer score must be between 1 and 10. Reviewer: {reviewer.Name}.";
                return false;
            }

            if (reviewer.Findings.Count == 0)
            {
                error = $"Reviewer findings are required. Reviewer: {reviewer.Name}.";
                return false;
            }

            if (reviewer.Findings.Any(string.IsNullOrWhiteSpace))
            {
                error = $"Reviewer findings cannot contain empty values. Reviewer: {reviewer.Name}.";
                return false;
            }
        }

        if (response.RecommendedActions.Count == 0)
        {
            error = "Recommended actions are required.";
            return false;
        }

        if (response.RecommendedActions.Any(string.IsNullOrWhiteSpace))
        {
            error = "Recommended actions cannot contain empty values.";
            return false;
        }

        return HasAllExpectedReviewers(response, out error);
    }

    private static bool HasAllExpectedReviewers(CodeReviewResponse response, out string error)
    {
        var actualReviewers = response.Reviewers
            .Select(reviewer => reviewer.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var expectedReviewer in ExpectedReviewers)
        {
            if (!actualReviewers.Contains(expectedReviewer))
            {
                error = $"Missing reviewer: {expectedReviewer}.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}