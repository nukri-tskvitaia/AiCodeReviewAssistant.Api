namespace AiCodeReviewAssistant.Api.Models;

public sealed class CodeReviewerRequest
{
    public string Language { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public ReviewerType ReviewerType { get; init; }
}
