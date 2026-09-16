# AI Engineering Manager Copilot

AI Engineering Manager Copilot is an engineering intelligence platform designed to help Engineering Managers understand team health, identify delivery and engineering risks, and turn engineering metrics into concrete actions.

The goal is not to replace engineering leadership, but to provide an evidence-based assistant for:

* understanding engineering health
* detecting delivery and quality risks
* identifying bottlenecks
* generating actionable recommendations
* supporting Engineering Manager decision-making
* reducing the time spent collecting and interpreting engineering data

---

## 🎯 Vision

Engineering Managers often have access to many engineering metrics but lack a simple way to transform them into a coherent picture of team health.

AI Engineering Manager Copilot aims to create a continuous loop:

```text
Engineering Data
       ↓
     Metrics
       ↓
   Health Score
       ↓
    Insights
       ↓
     Risks
       ↓
   AI Analysis
       ↓
     Actions
       ↓
 Engineering Decisions
       ↓
     Verification
```

The long-term objective is to provide an AI-powered Engineering Management cockpit combining quantitative engineering data with contextual analysis.

---

## 🚀 Current capabilities

### Engineering reports

The application can generate an engineering health report for a team over a defined period.

A report currently includes:

* overall engineering health score
* health level
* executive summary
* engineering metrics
* detected insights
* identified risks
* recommended engineering actions

Example:

```text
Engineering Health
────────────────────────────
Overall score: 72/100
Health level: Good

Insights
────────────────────────────
• High cycle time
• Slow PR reviews
• Low deployment frequency

Risks
────────────────────────────
• Delivery bottleneck
• Increased change failure risk

Actions
────────────────────────────
1. Improve PR review turnaround
2. Reduce cycle time
3. Increase deployment frequency
```

---

## 🤖 AI Engineering Analysis

The application supports an LLM-based analysis layer.

The AI receives a structured engineering context containing:

* reporting period
* overall score
* executive summary
* engineering metrics
* existing insights
* detected risks

The LLM then produces:

* executive analysis
* additional insights
* recommended actions
* action priorities

The application exposes the provider behind an abstraction:

```csharp
public interface ILlmProvider
{
    Task<LlmAnalysisResult> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken);
}
```

This keeps the application independent from the underlying LLM provider.

---

## 🧪 Fake LLM provider

Development and automated tests do not require an external AI API.

A `FakeLlmProvider` is available for deterministic local execution.

This allows the application to be developed and tested without:

* an OpenAI API key
* network access
* API costs
* non-deterministic LLM responses

The real provider can therefore be introduced independently from the core application logic.

---

## 🔐 OpenAI integration

An `OpenAiLlmProvider` is available as the real LLM implementation.

The provider currently validates:

* API key configuration
* model configuration
* empty LLM responses
* invalid JSON responses

The application does not require an OpenAI API key when using the fake provider.

> Never commit an API key to Git.

Use local configuration or .NET User Secrets for development.

---

## 📊 Supported engineering metrics

The current domain contains metrics such as:

* Cycle Time
* PR Review Time
* Deployment Frequency
* Change Failure Rate
* Lead Time
* Open PRs
* Merged PRs
* Blocked Items

These metrics are used to identify engineering insights and risks.

The metric model is intentionally extensible so that additional engineering signals can be introduced without changing the core reporting architecture.

---

## ⚠️ Risk detection

The reporting engine detects engineering risks based on unhealthy metrics.

Examples include:

```text
High change failure rate
Low deployment frequency
High cycle time
High PR review time
High lead time
High number of blocked items
```

Risks have:

* category
* title
* description
* severity

Supported severity levels include:

```text
Critical
High
Medium
Low
```

---

## 🎯 Engineering actions

Reports can generate concrete engineering actions from detected insights.

Actions contain:

* title
* description
* priority
* status
* report association

The application currently limits generated report actions to a maximum of three.

This is intentional.

The goal is to avoid producing a large backlog of recommendations and instead focus the Engineering Manager on the most important actions.

---

## 🏗️ Architecture

The project follows a pragmatic Clean Architecture approach.

```text
src/
│
├── AiEngineeringManagerCopilot.Domain
│   ├── Entities
│   ├── Enums
│   └── Domain rules
│
├── AiEngineeringManagerCopilot.Application
│   ├── AI
│   ├── Reports
│   ├── Metrics
│   ├── Risks
│   ├── Teams
│   └── Abstractions
│
├── AiEngineeringManagerCopilot.Infrastructure
│   ├── Persistence
│   ├── AI
│   └── Repositories
│
└── AiEngineeringManagerCopilot.Api
    ├── Endpoints
    ├── Dependency Injection
    └── HTTP configuration
```

### Dependency direction

```text
API
 │
 ▼
Application
 │
 ▼
Domain

Infrastructure
 │
 ├── Application
 └── Domain
```

The Domain layer remains independent from infrastructure concerns.

The Application layer contains the business use cases and abstractions.

Infrastructure implements those abstractions.

The API exposes the application capabilities through HTTP endpoints.

---

## 🧩 Key design decisions

### LLM abstraction

The application depends on:

```csharp
ILlmProvider
```

rather than directly depending on OpenAI.

This provides:

* provider independence
* deterministic testing
* easier provider replacement
* lower coupling
* simpler local development

---

### Repository abstractions

Persistence is accessed through application-level repository abstractions.

For example:

```csharp
IEngineeringReportRepository
IEngineeringMetricRepository
IEngineeringRiskRepository
IAIAnalysisRepository
```

This prevents the application layer from becoming coupled to Entity Framework Core.

---

### AI analysis idempotency

An AI analysis is associated with a report.

Before creating a new analysis, the application checks whether an analysis already exists:

```text
Report
  │
  └── AI Analysis
```

If an analysis already exists, it is returned rather than generating another analysis.

This prevents duplicate AI analyses and unnecessary LLM calls.

---

## 🌐 API

Current report endpoints include:

```http
POST /teams/{teamId}/reports
```

Generate an engineering report.

```http
GET /teams/{teamId}/reports
```

Retrieve reports for a team.

```http
GET /teams/{teamId}/reports/{reportId}
```

Retrieve a specific report.

```http
POST /teams/{teamId}/reports/{reportId}/analyze
```

Generate an AI analysis for an engineering report.

---

## 🛠️ Technology stack

### Backend

* .NET 10
* C#
* ASP.NET Core Minimal APIs
* Entity Framework Core

### Testing

* xUnit
* FluentAssertions
* Integration testing with `WebApplicationFactory`

### AI

* OpenAI Responses API
* Pluggable `ILlmProvider`
* Fake LLM provider for local development and tests

### Development

* Rider / Visual Studio Code
* Git
* macOS
* .NET CLI

---

## 🧪 Testing

The project currently contains:

```text
244 tests
244 passed
0 failed
0 skipped
```

Run the complete test suite:

```bash
dotnet test
```

Run a specific test:

```bash
dotnet test \
  tests/AiEngineeringManagerCopilot.IntegrationTests \
  --filter "FullyQualifiedName~AnalyzeReport_ShouldReturnAIAnalysis"
```

The test suite covers:

* team creation
* report generation
* report persistence
* report retrieval
* team isolation
* insight generation
* risk generation
* action generation
* action prioritization
* AI analysis
* duplicate AI analysis prevention
* LLM provider behavior
* missing API key handling

---

## 💻 Local development

### Prerequisites

Install:

* .NET 10 SDK
* Git

Verify the SDK:

```bash
dotnet --version
```

Restore dependencies:

```bash
dotnet restore
```

Build:

```bash
dotnet build
```

Run tests:

```bash
dotnet test
```

Run the API:

```bash
dotnet run --project src/AiEngineeringManagerCopilot.Api
```

---

## ⚙️ Configuration

The application supports selecting the LLM provider through configuration.

Example:

```json
{
  "Llm": {
    "Provider": "Fake",
    "ApiKey": "",
    "Model": ""
  }
}
```

For local development, the recommended default is:

```text
Provider = Fake
```

This allows the complete application to run without an external API.

For OpenAI:

```text
Provider = OpenAI
ApiKey   = <secret>
Model    = <model>
```

Do not store the real API key in source control.

---

## 🔒 Secrets

For local development, use .NET User Secrets or environment variables.

Example:

```bash
dotnet user-secrets set "Llm:ApiKey" "YOUR_API_KEY"
```

Never commit:

```text
.env
*.secrets.json
appsettings.Local.json
```

The repository contains a `.gitignore` covering local configuration, build artifacts, IDE files, databases and secrets.

---

## 📁 Project structure

```text
AiEngineeringManagerCopilot/
│
├── src/
│   ├── AiEngineeringManagerCopilot.Domain/
│   ├── AiEngineeringManagerCopilot.Application/
│   ├── AiEngineeringManagerCopilot.Infrastructure/
│   └── AiEngineeringManagerCopilot.Api/
│
├── tests/
│   ├── AiEngineeringManagerCopilot.UnitTests/
│   └── AiEngineeringManagerCopilot.IntegrationTests/
│
├── .gitignore
├── README.md
└── AiEngineeringManagerCopilot.sln
```

---

## 🧭 Roadmap

### Phase 1 — Engineering intelligence foundation

* [x] Team management
* [x] Engineering metrics
* [x] Engineering health score
* [x] Engineering insights
* [x] Risk detection
* [x] Recommended actions
* [x] Engineering reports
* [x] Report persistence
* [x] API endpoints
* [x] Integration tests

### Phase 2 — AI Engineering Manager

* [x] `ILlmProvider`
* [x] Fake LLM provider
* [x] OpenAI provider
* [x] AI report analysis
* [x] AI insights
* [x] AI actions
* [x] Duplicate analysis prevention
* [ ] Structured LLM output validation
* [ ] Better prompt engineering
* [ ] AI confidence / evidence
* [ ] Analysis history

### Phase 3 — Engineering Management cockpit

* [ ] Web dashboard
* [ ] Team health overview
* [ ] Metric trends
* [ ] Risk dashboard
* [ ] Action tracking
* [ ] Historical comparisons
* [ ] Team-level filtering
* [ ] Engineering health evolution

### Phase 4 — Engineering data integrations

Potential integrations:

* [ ] GitHub
* [ ] GitLab
* [ ] Jira
* [ ] Linear
* [ ] Azure DevOps
* [ ] Datadog
* [ ] CI/CD platforms

The objective is to automatically collect engineering signals rather than relying exclusively on manually entered metrics.

### Phase 5 — AI Engineering Manager Copilot

Long-term capabilities:

* contextual team analysis
* recurring engineering reviews
* automatic detection of delivery anomalies
* engineering risk forecasting
* action follow-up
* post-mortem assistance
* engineering decision support
* team health trends
* personalized recommendations for Engineering Managers

---

## 🧠 Product principles

The project follows several principles.

### Evidence before opinion

Recommendations should be grounded in observable engineering data.

### Fewer, better actions

The goal is not to generate hundreds of recommendations.

The system should help identify the few actions that are most relevant.

### AI as a copilot

The AI should support Engineering Manager decision-making rather than replace it.

### Explainability

AI recommendations should ultimately be connected to the metrics, insights and risks that generated them.

### Human ownership

Engineering decisions remain the responsibility of engineering leadership.

---

## 🚧 Current limitations

This project is currently an MVP.

Known limitations include:

* LLM output still requires stronger schema validation
* engineering thresholds are currently rule-based
* metrics are not yet automatically imported from external systems
* there is no production dashboard yet
* authentication and authorization are not yet implemented
* AI analysis is currently report-based rather than continuously evaluated
* historical trend analysis is limited

These limitations are intentional and will be addressed incrementally.

---

## 📈 Quality goals

The project aims to maintain:

```text
Build       → 0 errors
Tests       → 100% green
Architecture→ Clear boundaries
AI          → Provider independent
Secrets     → Never committed
Delivery    → Small incremental changes
```

The current baseline is:

```text
244 tests
244 passed
0 failed
```

---

## 📄 License

License to be defined.
