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
    private const string ToolName = "analyze_code_review";
    private readonly ClaudeOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<CodeReviewResponse> AnalyzeCodeAsync(
        CodeReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestBody = CreateRequestBody(request);

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

        return ExtractCodeReviewResponse(responseText);
    }

    private object CreateRequestBody(CodeReviewRequest request)
    {
        return new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,

            system = """
            You are an expert multi-role software code review system.

            You must act as:
            - Security Reviewer
            - Performance Reviewer
            - Clean Code Reviewer

            Analyze the provided code and return structured review results using the provided tool.

            Rules:
            - Do not invent issues.
            - Be concise and technical.
            - Findings must be actionable.
            - Scores must be between 1 and 10.
            """,

            tools = new[]
            {
                new
                {
                    name = ToolName,
                    description = "Structured software code review result.",
                    input_schema = new
                    {
                        type = "object",

                        properties = new
                        {
                            summary = new
                            {
                                type = "string"
                            },

                            reviewers = new
                            {
                                type = "array",

                                items = new
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
                                            type = "integer"
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
                            },

                            recommendedActions = new
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
                            "summary",
                            "reviewers",
                            "recommendedActions"
                        }
                    }
                }
            },

            tool_choice = new
            {
                type = "tool",
                name = ToolName
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

    private static CodeReviewResponse ExtractCodeReviewResponse(string responseText)
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

            if (!string.Equals(nameProperty.GetString(), ToolName, StringComparison.Ordinal))
                continue;

            var input = block.GetProperty("input");

            return input.Deserialize<CodeReviewResponse>(JsonOptions)
                   ?? throw new InvalidOperationException("Failed to deserialize code review response.");
        }

        throw new InvalidOperationException("Claude did not return the expected tool_use block.");
    }
}