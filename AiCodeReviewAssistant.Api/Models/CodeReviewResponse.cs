namespace AiCodeReviewAssistant.Api.Models;

public sealed class CodeReviewResponse
{
    public string Summary { get; set; } = string.Empty;

    public List<ReviewerResult> Reviewers { get; set; } = [];

    public List<string> RecommendedActions { get; set; } = [];
}