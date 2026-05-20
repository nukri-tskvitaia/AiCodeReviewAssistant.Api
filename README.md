# AI Code Review Assistant API

A multi-agent ASP.NET Core Web API that uses Anthropic Claude to perform AI-powered software code reviews.

The application demonstrates role-specialized AI reviewers, orchestration patterns, structured tool output, aggregation workflows, backend validation, timeout handling, and partial failure resilience.

---

# Features

- ASP.NET Core Web API
- Anthropic Claude API integration
- Multi-agent orchestration architecture
- Role-specialized reviewers
  - Security Reviewer
  - Performance Reviewer
  - Clean Code Reviewer
- Aggregator AI reviewer
- Structured tool/function calling
- Strongly typed configuration using `IOptions`
- Request validation
- Response validation
- Swagger/OpenAPI support
- Parallel reviewer execution
- Reviewer timeout handling
- Partial failure handling
- Safe JSON deserialization
- AI response aggregation
- Clean architecture structure

---

# Architecture

```text
Controller
→ Orchestrator
   → Security Reviewer Claude call
   → Performance Reviewer Claude call
   → Clean Code Reviewer Claude call
→ Aggregator Claude call
→ Validation
→ API response
```

---

# Example Request

```json
{
  "language": "javascript",
  "code": "function getUser(id) {\n  const query = \"SELECT * FROM users WHERE id = \" + id;\n  return db.execute(query);\n}"
}
```

---

# Example Response

```json
{
  "summary": "Critical security vulnerabilities identified in database query implementation.",
  "reviewers": [
    {
      "name": "Security Reviewer",
      "score": 2,
      "findings": [
        "SQL Injection vulnerability detected."
      ]
    },
    {
      "name": "Performance Reviewer",
      "score": 4,
      "findings": [
        "SELECT * retrieves unnecessary columns."
      ]
    },
    {
      "name": "Clean Code Reviewer",
      "score": 6,
      "findings": [
        "Missing error handling."
      ]
    }
  ],
  "recommendedActions": [
    "Use parameterized queries.",
    "Replace SELECT * with explicit column names.",
    "Add error handling."
  ]
}
```

---

# Technologies Used

- ASP.NET Core 9 Web API
- C#
- Anthropic Claude API
- Swagger / OpenAPI
- System.Text.Json
- Options Pattern (`IOptions<T>`)

---

# Project Structure

```text
Controllers/
Models/
Options/
Services/
Services/Reviewers/
Validation/
Exceptions/
```

---

# AI Concepts Demonstrated

| Concept | Used |
|---|---|
| Role-specialized agents | Yes |
| Orchestrator pattern | Yes |
| Multi-agent workflow | Yes |
| Structured tool/function calling | Yes |
| Aggregator agent | Yes |
| Backend validation | Yes |
| Parallel execution | Yes |
| Timeout handling | Yes |
| Partial failure handling | Yes |

---

# Configuration

Store configuration securely using User Secrets or environment variables.

Example configuration:

```json
{
  "Claude": {
    "ApiKey": "your_claude_api_key",
    "Version": "2023-06-01",
    "Model": "claude-haiku-4-5-20251001",
    "MaxTokens": 1500,
    "Temperature": 0.1,
    "MessagesEndpointUrl": "https://api.anthropic.com/v1/messages"
  }
}
```

---

# Running the Project

## Clone the repository

```bash
git clone <your_repo_url>
```

## Navigate to project

```bash
cd AiCodeReviewAssistant.Api
```

## Restore packages

```bash
dotnet restore
```

## Run the API

```bash
dotnet run
```

Swagger UI will open automatically.

---

# API Endpoint

## Analyze Code

```http
POST /api/code-reviews/analyze
```

---

# Validation

The application validates:

- Request model input
- Reviewer names
- Reviewer scores
- Required AI response fields
- Structured tool outputs
- JSON deserialization integrity

---

# Resilience Features

## Parallel Execution Timeout

Reviewer execution is protected using cancellation tokens and timeout handling.

```csharp
using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
```

---

## Partial Failure Handling

If one reviewer fails or times out:

- Other reviewers still complete
- Aggregation still happens
- The API still returns partial results

Example:

```text
2 reviewers succeed
1 reviewer fails
→ Final response still returned
```

---

# Notes

This project demonstrates:

- AI orchestration patterns
- Multi-agent workflows
- Prompt engineering
- Structured AI outputs
- Tool/function calling
- Production-oriented AI backend design
- AI resilience strategies
- Backend validation techniques

The project intentionally balances simplicity and architecture quality while remaining lightweight enough for experimentation and portfolio usage.