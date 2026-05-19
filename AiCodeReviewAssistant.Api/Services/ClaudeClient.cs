using AiCodeReviewAssistant.Api.Exceptions;
using AiCodeReviewAssistant.Api.Models;
using AiCodeReviewAssistant.Api.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace AiCodeReviewAssistant.Api.Services;

public sealed class ClaudeClient(
    HttpClient httpClient,
    IOptions<ClaudeOptions> options)
{
    private const string ReviewerToolName = "review_code";
    private const string AggregatorToolName = "aggregate_code_review";

    private readonly ClaudeOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ReviewerResult> ReviewCodeAsync(
    CodeReviewerRequest request,
    CancellationToken cancellationToken = default)
    {
        var requestBody = CreateReviewerRequestBody(request);

        var responseText = await SendClaudeRequestAsync(
            requestBody,
            cancellationToken);

        return ExtractToolInput<ReviewerResult>(responseText, ReviewerToolName);
    }

    public async Task<CodeReviewResponse> AggregateReviewAsync(
        List<ReviewerResult> reviewerResults,
        CancellationToken cancellationToken = default)
    {
        var requestBody = CreateAggregatorRequestBody(reviewerResults);

        var responseText = await SendClaudeRequestAsync(
            requestBody,
            cancellationToken);

        return ExtractToolInput<CodeReviewResponse>(responseText, AggregatorToolName);
    }

    private async Task<string> SendClaudeRequestAsync(
        object requestBody,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            _options.MessagesEndpointUrl);

        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", _options.Version);

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new AnthropicException((int)response.StatusCode, responseText);

        return responseText;
    }

    private object CreateReviewerRequestBody(CodeReviewerRequest request)
    {
        return new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,

            system = GetReviewerSystemPrompt(request.ReviewerType),

            tools = new[]
            {
            new
            {
                name = ReviewerToolName,
                description = "Returns a structured code review result from one specialized reviewer.",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new
                        {
                            type = "string"
                        },
                        score = new
                        {
                            type = "integer",
                            minimum = 1,
                            maximum = 10
                        },
                        findings = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "string"
                            }
                        }
                    },
                    required = new[]
                    {
                        "name",
                        "score",
                        "findings"
                    }
                }
            }
        },

            tool_choice = new
            {
                type = "tool",
                name = ReviewerToolName
            },

            messages = new[]
            {
            new
            {
                role = "user",
                content = $"""
                Language:
                {request.Language}

                Code:
                {request.Code}
                """
            }
        }
        };
    }

    private static string GetReviewerSystemPrompt(ReviewerType reviewerType)
    {
        return reviewerType switch
        {
       ReviewerType.Security => """
        You are a Security Code Reviewer.

        Review only security-related issues.

        Focus on:
        - Injection vulnerabilities
        - Authentication and authorization mistakes
        - Sensitive data exposure
        - Unsafe input handling
        - Unsafe external calls
        - Dangerous file, network, or database operations

        Rules:
        - Do not comment on performance unless it directly affects security.
        - Do not comment on style unless it creates security risk.
        - Do not invent vulnerabilities.
        - If there are no serious security issues, say so clearly.
        - Return the reviewer name exactly as "Security Reviewer".
        """,

       ReviewerType.Performance => """
        You are a Performance Code Reviewer.

        Review only performance-related issues.

        Focus on:
        - Unnecessary allocations
        - Inefficient loops
        - Blocking I/O
        - Expensive repeated operations
        - Poor database query efficiency
        - Scalability bottlenecks

        Rules:
        - Do not exaggerate minor style preferences as performance problems.
        - Do not claim LINQ is faster than loops unless there is a clear reason.
        - Do not comment on security unless it directly affects performance.
        - If performance is acceptable, say so clearly.
        - Return the reviewer name exactly as "Performance Reviewer".
        """,

       ReviewerType.CleanCode => """
        You are a Clean Code Reviewer.

        Review only readability, maintainability, naming, structure, and simplicity.

        Focus on:
        - Naming clarity
        - Method size and responsibility
        - Duplication
        - Error handling readability
        - Maintainability
        - Testability

        Rules:
        - Do not comment on security unless it affects maintainability.
        - Do not comment on performance unless it affects readability or structure.
        - Do not exaggerate optional preferences as serious problems.
        - If the code is clean enough, say so clearly.
        - Return the reviewer name exactly as "Clean Code Reviewer".
        """,

            _ => throw new ArgumentOutOfRangeException(nameof(reviewerType), reviewerType, null)
        };
    }

    private static T ExtractToolInput<T>(string responseText, string toolName)
    {
        using var document = JsonDocument.Parse(responseText);

        var content = document.RootElement.GetProperty("content");

        foreach (var block in content.EnumerateArray())
        {
            if (!block.TryGetProperty("type", out var typeProperty))
                continue;

            if (!string.Equals(typeProperty.GetString(), "tool_use", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!block.TryGetProperty("name", out var nameProperty))
                continue;

            if (!string.Equals(nameProperty.GetString(), toolName, StringComparison.Ordinal))
                continue;

            var input = block.GetProperty("input");

            return input.Deserialize<T>(JsonOptions)
                   ?? throw new InvalidOperationException($"Failed to deserialize tool input for tool: {toolName}.");
        }

        throw new InvalidOperationException($"Claude did not return the expected tool_use block: {toolName}.");
    }

    private object CreateAggregatorRequestBody(List<ReviewerResult> reviewerResults)
    {
        return new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,

            system = """
            You are a Code Review Aggregator.

            Your job is to combine multiple specialized reviewer results into one final response.

            Rules:
            - Keep the original reviewer results unchanged.
            - Create a concise final summary.
            - Recommended actions must be actual actions, not copied findings.
            - Do not include positive findings as recommended actions.
            - Deduplicate similar actions.
            - Prioritize critical issues first.
            - Keep recommended actions short and practical.
            """,

            tools = new[]
            {
            new
            {
                name = "aggregate_code_review",
                description = "Creates the final aggregated code review response.",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        summary = new { type = "string" },
                        reviewers = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    name = new { type = "string" },
                                    score = new { type = "integer", minimum = 1, maximum = 10 },
                                    findings = new
                                    {
                                        type = "array",
                                        items = new { type = "string" }
                                    }
                                },
                                required = new[] { "name", "score", "findings" }
                            }
                        },
                        recommendedActions = new
                        {
                            type = "array",
                            items = new { type = "string" }
                        }
                    },
                    required = new[] { "summary", "reviewers", "recommendedActions" }
                }
            }
        },

            tool_choice = new
            {
                type = "tool",
                name = "aggregate_code_review"
            },

            messages = new[]
            {
            new
            {
                role = "user",
                content = $"""
                Aggregate these reviewer results into a final code review response.

                Reviewer results:
                {JsonSerializer.Serialize(reviewerResults, JsonOptions)}
                """
            }
        }
        };
    }
}