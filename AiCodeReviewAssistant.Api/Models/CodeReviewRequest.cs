using System.ComponentModel.DataAnnotations;

namespace AiCodeReviewAssistant.Api.Models;

public sealed class CodeReviewRequest
{
    [Required]
    [MinLength(1)]
    public string Language { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Code { get; set; } = string.Empty;
}