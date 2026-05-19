namespace AiCodeReviewAssistant.Api.Models;

public sealed class ReviewerResult
{
    public string Name { get; set; } = string.Empty;

    public int Score { get; set; }

    public List<string> Findings { get; set; } = [];
}