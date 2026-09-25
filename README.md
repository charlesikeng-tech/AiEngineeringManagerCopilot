# AI Engineering Manager Copilot

AI Engineering Manager Copilot is an engineering intelligence platform designed to help Engineering Managers understand team health, identify delivery and engineering risks, and turn engineering data into concrete actions.

The goal is not to replace engineering leadership.

The product acts as an evidence-based copilot that helps Engineering Managers:

- understand engineering health;
- detect delivery and quality risks;
- identify bottlenecks;
- monitor engineering metrics;
- connect GitHub and Jira engineering data;
- generate actionable recommendations;
- support engineering decisions;
- reduce the time spent collecting and interpreting engineering information.

---

## 🎯 Vision

Engineering Managers have access to large amounts of engineering data across tools such as GitHub, Jira and CI/CD platforms.

The difficult part is not collecting data.

The difficult part is turning that data into a coherent understanding of:

- what is happening;
- why it matters;
- what is becoming risky;
- what should be done next.

AI Engineering Manager Copilot aims to create the following continuous loop:

```text
GitHub / Jira
      ↓
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

The long-term objective is to answer a simple question:

> What are the three most important things an Engineering Manager should do this week?

The answer should be data-driven, contextual, explainable, actionable and verifiable.

---

# 🚀 Current capabilities

The current backend MVP already supports:

- team management;
- team members;
- GitHub connection management;
- GitHub repository synchronization;
- GitHub pull request synchronization;
- GitHub pull request review synchronization;
- GitHub deployment synchronization;
- Jira connection management;
- Jira connection validation;
- Jira issue synchronization;
- engineering metric calculation;
- engineering health scoring;
- engineering reports;
- engineering insights;
- engineering risk detection;
- recommended engineering actions;
- AI-based report analysis;
- OpenAI integration;
- deterministic fake LLM execution;
- PostgreSQL persistence;
- automated unit and integration testing.

---

# 🔌 Engineering data integrations

## GitHub

GitHub is currently used as an engineering data source.

The application can synchronize:

```text
GitHub
 │
 ├── Repositories
 ├── Pull Requests
 ├── Pull Request Reviews
 └── Deployments
```

The synchronization flow is:

```text
GitHub API
    ↓
IGitHubClient
    ↓
GitHub Sync Services
    ↓
Repositories
Pull Requests
Reviews
Deployments
    ↓
PostgreSQL
    ↓
Engineering Metrics
```

GitHub credentials are stored through the connection abstraction and the access token is protected before persistence.

### GitHub metrics

GitHub currently provides the source data for:

| Metric               | GitHub data             |
| -------------------- | ----------------------- |
| Cycle Time           | Pull Requests           |
| PR Review Time       | Pull Requests + Reviews |
| Deployment Frequency | Deployments             |
| Change Failure Rate  | Deployments             |
| Open PRs             | Pull Requests           |
| Merged PRs           | Pull Requests           |

---

## Jira

Jira is also implemented as an engineering data source.

A team can configure a Jira connection using:

- Jira base URL;
- account email;
- API token;
- project key.

The application can validate the connection before synchronization.

The synchronization flow is:

```text
Jira Cloud
    ↓
IJiraClient
    ↓
Jira REST API
    ↓
JiraSyncService
    ↓
JiraWorkItem
    ↓
PostgreSQL
    ↓
Engineering Metrics
```

Jira issue synchronization currently stores:

```text
ExternalId
Key
Summary
Status
AssigneeExternalId
CreatedAt
DoneAt
IsBlocked
```

Issue synchronization supports pagination through Jira's enhanced JQL search API.

For the current MVP:

```text
Jira resolutiondate
        ↓
JiraIssue.DoneAt
        ↓
JiraWorkItem.DoneAt
```

`resolutiondate` is therefore currently used as the completion timestamp.

A future improvement can use Jira changelog information to determine the exact workflow transition into a completed state.

### Jira metrics

Jira currently provides the source data for:

| Metric        | Jira data                         |
| ------------- | --------------------------------- |
| Lead Time     | Work item CreatedAt → DoneAt      |
| Blocked Items | Jira work items marked as blocked |

---

# 📊 Engineering metrics

The platform currently supports eight engineering metrics.

| Metric               | Source | Current meaning                           |
| -------------------- | ------ | ----------------------------------------- |
| Cycle Time           | GitHub | Average duration of merged pull requests  |
| PR Review Time       | GitHub | Time between PR creation and first review |
| Deployment Frequency | GitHub | Successful deployments during the period  |
| Change Failure Rate  | GitHub | Failed deployments / total deployments    |
| Lead Time            | Jira   | Work item creation → completion           |
| Open PRs             | GitHub | Open pull requests                        |
| Merged PRs           | GitHub | Merged pull requests                      |
| Blocked Items        | Jira   | Blocked Jira work items                   |

Metrics are persisted as `EngineeringMetric` records and associated with:

```text
Team
MetricType
Value
PeriodStart
PeriodEnd
CreatedAt
```

The metric architecture is intentionally extensible so additional signals can be introduced later.

---

# ❤️ Engineering health

Metrics feed an engineering health scoring layer.

```text
Engineering Metrics
        ↓
Health Score
        ↓
Health Level
```

The current health levels are:

```text
Excellent
Healthy
Needs Attention
At Risk
Critical
```

The health score provides a summarized view of engineering conditions for a selected reporting period.

---

# 📄 Engineering reports

The application can generate engineering reports for a team and a selected period.

A report contains:

- reporting period;
- overall engineering health score;
- health level;
- executive summary;
- engineering insights;
- detected risks;
- recommended actions.

Example:

```text
Engineering Health
────────────────────────────

Overall score: 72/100
Health level: Needs Attention

Insights
────────────────────────────

• High cycle time
• Slow PR reviews
• Low deployment frequency

Risks
────────────────────────────

• Delivery bottleneck
• Increased delivery risk

Actions
────────────────────────────

1. Improve PR review turnaround
2. Reduce cycle time
3. Improve deployment flow
```

Reports are persisted and can later be retrieved for the owning team.

---

# ⚠️ Risk detection

The application contains a rule-based engineering risk engine.

It can detect signals such as:

```text
High change failure rate
Low deployment frequency
High cycle time
High PR review time
High lead time
High number of blocked items
```

An `EngineeringRisk` contains information such as:

```text
Category
Title
Description
Severity
```

Supported severity levels include:

```text
Critical
High
Medium
Low
```

The rules are intentionally deterministic and explainable.

AI analysis is an additional layer and does not replace deterministic engineering rules.

---

# 🎯 Engineering actions

Engineering reports can generate concrete actions from detected engineering conditions.

An `EngineeringAction` contains information such as:

```text
Title
Description
Priority
Status
Report association
```

The report generation process intentionally limits the number of recommended actions.

The objective is:

> Fewer, better actions.

The product should help an Engineering Manager identify the most important interventions rather than generate another large backlog.

---

# 🤖 AI Engineering Analysis

The application contains an AI analysis layer built behind an abstraction.

```csharp
public interface ILlmProvider
{
    Task<LlmAnalysisResult> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken);
}
```

The application therefore does not directly depend on a specific LLM implementation.

The AI receives structured engineering context including:

- reporting period;
- overall score;
- executive summary;
- engineering metrics;
- existing insights;
- detected risks.

It can produce:

- executive analysis;
- additional insights;
- recommended actions;
- action priorities.

The AI layer uses structured output parsing so application behavior remains controlled by the application rather than by free-form LLM text.

---

# 🧪 Fake LLM provider

A deterministic `FakeLlmProvider` is available.

It allows the complete AI workflow to be tested without:

- an OpenAI API key;
- network access;
- API costs;
- non-deterministic model responses.

This provider is used extensively by automated tests.

```text
Application
     ↓
 ILlmProvider
   ↙     ↘
Fake    OpenAI
```

---

# 🔐 OpenAI integration

A real OpenAI implementation is available behind `ILlmProvider`.

The implementation uses the official OpenAI .NET package.

The OpenAI integration handles configuration and validates conditions such as:

- missing API key;
- missing/invalid model configuration;
- empty model responses;
- invalid structured responses.

OpenAI is optional.

The application can run locally and execute its automated tests using the fake provider.

Never commit an OpenAI API key to Git.

---

# 🏗️ Architecture

The backend is implemented as a pragmatic modular monolith using Clean Architecture principles.

```text
┌─────────────────────────────────────┐
│                 API                 │
│                                     │
│ Minimal APIs / HTTP endpoints       │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│            Application              │
│                                     │
│ Use cases                           │
│ Services                            │
│ Metrics                             │
│ Health                              │
│ Reports                             │
│ AI                                  │
│ Integration abstractions            │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│               Domain                │
│                                     │
│ Entities                            │
│ Enums                               │
│ Domain concepts                     │
└─────────────────────────────────────┘

             ▲
             │ implements abstractions
             │

┌─────────────────────────────────────┐
│           Infrastructure            │
│                                     │
│ EF Core                             │
│ PostgreSQL                          │
│ Repositories                        │
│ GitHub client                       │
│ Jira client                         │
│ OpenAI provider                     │
│ Secret protection                   │
└─────────────────────────────────────┘
```

---

## Dependency direction

The project dependencies follow:

```text
API
 ├── Application
 └── Infrastructure

Infrastructure
 ├── Application
 └── Domain

Application
 └── Domain

Domain
 └── no infrastructure dependency
```

The Domain layer does not depend on:

- Entity Framework Core;
- PostgreSQL;
- GitHub;
- Jira;
- OpenAI;
- ASP.NET Core.

External systems are accessed through abstractions.

---

# 🧩 Main application areas

The current application is organized around several capabilities.

```text
Application
│
├── Teams
├── Team Members
├── GitHub
├── Jira
├── Metrics
├── Health
├── Reports
├── Risks
├── Actions
└── AI
```

Infrastructure provides the implementations required by those application capabilities.

---

# 🗃️ Domain model

The current backend contains domain entities including:

```text
User

Team
└── TeamMember

GitHubConnection
Repository
└── PullRequest
    └── PullRequestReview

Deployment

JiraConnection
└── JiraWorkItem

EngineeringMetric

EngineeringReport
├── EngineeringReportInsight
├── EngineeringRisk
└── EngineeringAction

AIAnalysis
├── AIAnalysisInsight
└── AIAnalysisAction
```

This separates external engineering data from the intelligence generated from that data.

---

# 🗄️ Persistence

The application uses:

```text
Entity Framework Core
        ↓
PostgreSQL
```

`AppDbContext` persists the main application entities.

Database schema changes are managed using EF Core migrations.

Examples of persisted engineering data include:

```text
teams
team_members

github_connections
repositories
pull_requests
pull_request_reviews
deployments

jira_connections
jira_work_items

engineering_metrics
engineering_reports
engineering_risks
engineering_actions

AI analyses
```

Uniqueness constraints are used for external synchronized data to prevent duplicate records during repeated synchronization.

Examples include repository/external identifiers and Jira team/external issue identifiers.

---

# 🔄 Synchronization strategy

GitHub and Jira synchronization are designed to be repeatable.

The general model is:

```text
External API
     ↓
Fetch
     ↓
Map
     ↓
Find existing entity
   ↙       ↘
Create    Update
   ↘       ↙
Persistence
     ↓
LastSyncAt
```

This allows synchronization to update existing data instead of blindly creating duplicates.

---

# 🌐 API

The application exposes ASP.NET Core Minimal APIs.

The main API areas currently include:

```text
Teams
Team Members
GitHub
Jira
Engineering Metrics
Engineering Reports
AI Analysis
```

## Engineering metrics

Metric calculation endpoints are exposed under:

```http
/teams/{teamId}/metrics
```

Available metric operations include:

```http
POST /teams/{teamId}/metrics/cycle-time
POST /teams/{teamId}/metrics/pr-review-time
POST /teams/{teamId}/metrics/deployment-frequency
POST /teams/{teamId}/metrics/change-failure-rate
POST /teams/{teamId}/metrics/lead-time
POST /teams/{teamId}/metrics/open-prs
POST /teams/{teamId}/metrics/merged-prs
POST /teams/{teamId}/metrics/blocked-items
```

Metric endpoints accept a reporting period using:

```text
periodStart
periodEnd
```

---

## Engineering reports

```http
POST /teams/{teamId}/reports
```

Generate an engineering report.

```http
GET /teams/{teamId}/reports
```

Retrieve reports belonging to the team.

```http
GET /teams/{teamId}/reports/{reportId}
```

Retrieve a specific report.

```http
POST /teams/{teamId}/reports/{reportId}/analyze
```

Generate or retrieve an AI analysis for a report.

---

## GitHub integration

GitHub endpoints allow a team to:

```text
Configure connection
Retrieve connection
Delete connection
Test connection
Synchronize engineering data
```

The GitHub synchronization pipeline covers:

```text
Repositories
Pull Requests
Pull Request Reviews
Deployments
```

---

## Jira integration

Jira endpoints are grouped under:

```http
/teams/{teamId}/jira
```

Current operations include:

```http
POST /teams/{teamId}/jira
GET /teams/{teamId}/jira
DELETE /teams/{teamId}/jira
POST /teams/{teamId}/jira/test
POST /teams/{teamId}/jira/sync
```

The Jira synchronization endpoint imports Jira issues into `JiraWorkItem`.

---

# 🛠️ Technology stack

## Backend

- .NET 10
- C#
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL

## External integrations

- GitHub REST API
- Jira Cloud REST API
- OpenAI

## Testing

- xUnit
- FluentAssertions
- `WebApplicationFactory`
- PostgreSQL integration tests
- Fake GitHub client
- Fake Jira client
- Fake LLM provider

## API

- Minimal APIs
- OpenAPI / Swagger

## Development

- .NET CLI
- Git
- Docker / Docker Compose
- Visual Studio Code / Rider
- macOS / Linux-compatible CI

## CI/CD

- GitHub Actions

---

# 🧪 Testing

The solution contains two main automated test projects:

```text
AiEngineeringManagerCopilot.UnitTests
AiEngineeringManagerCopilot.IntegrationTests
```

The current local baseline is:

```text
153 unit tests
129 integration tests

282 total
282 passed
0 failed
0 skipped
```

The final total is also maintained automatically in the generated README status section.

Run all tests:

```bash
dotnet test
```

The automated suite covers areas including:

- team management;
- team members;
- repository persistence;
- GitHub connection management;
- GitHub synchronization;
- pull request synchronization;
- review synchronization;
- deployment synchronization;
- Jira connection management;
- Jira connection validation;
- Jira issue synchronization;
- metric calculators;
- metric endpoints;
- engineering health;
- engineering reports;
- risk generation;
- action generation;
- AI analysis;
- structured LLM parsing;
- duplicate AI analysis prevention;
- fake and real-provider configuration behavior.

Integration tests use PostgreSQL.

The test environment currently uses:

```text
Database: ai_engineering_manager_test
Port:     5433
```

---

# 💻 Local development

## Prerequisites

Install:

- .NET 10 SDK;
- PostgreSQL or Docker;
- Git.

Verify .NET:

```bash
dotnet --version
```

Restore:

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

# ⚙️ Configuration

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

For local development:

```text
Provider = Fake
```

This allows AI workflows to execute without an external API.

For OpenAI:

```text
Provider = OpenAI
ApiKey   = <secret>
Model    = <model>
```

---

# 🔒 Secrets

Secrets must never be committed to Git.

Sensitive integration values include:

```text
OpenAI API key
GitHub access token
Jira API token
```

For local .NET configuration, User Secrets or environment variables should be used.

Example:

```bash
dotnet user-secrets set "Llm:ApiKey" "YOUR_API_KEY"
```

Local secret/configuration files should remain outside source control.

---

# 📁 Project structure

```text
AiEngineeringManagerCopilot/
│
├── src/
│   │
│   ├── AiEngineeringManagerCopilot.Api/
│   │   └── Minimal API endpoints
│   │
│   ├── AiEngineeringManagerCopilot.Application/
│   │   ├── Abstractions/
│   │   ├── AI/
│   │   ├── GitHub/
│   │   ├── Jira/
│   │   ├── Metrics/
│   │   ├── Health/
│   │   ├── Reports/
│   │   └── Teams/
│   │
│   ├── AiEngineeringManagerCopilot.Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   │
│   └── AiEngineeringManagerCopilot.Infrastructure/
│       ├── Persistence/
│       ├── Repositories/
│       ├── GitHub/
│       ├── Jira/
│       └── AI/
│
├── tests/
│   ├── AiEngineeringManagerCopilot.UnitTests/
│   └── AiEngineeringManagerCopilot.IntegrationTests/
│
├── scripts/
│   └── update-readme.sh
│
├── .github/
│   └── workflows/
│
├── README.md
└── AiEngineeringManagerCopilot.sln
```

---

# 🔑 Key design decisions

## Modular monolith

The MVP remains a modular monolith.

No microservices are currently required.

This keeps deployment and development simple while preserving clear boundaries between application capabilities.

---

## External systems behind abstractions

The application does not expose external provider implementation details to the domain.

Examples include:

```text
IGitHubClient
IJiraClient
ILlmProvider
```

This allows:

- fake implementations in tests;
- provider replacement;
- lower coupling;
- deterministic local execution.

---

## Repository abstractions

Application services access persistence through repository abstractions rather than directly using `AppDbContext`.

Examples include:

```text
ITeamRepository
IPullRequestRepository
IPullRequestReviewRepository
IDeploymentRepository
IJiraConnectionRepository
IJiraWorkItemRepository
IEngineeringMetricRepository
IEngineeringReportRepository
```

EF Core implementations live in Infrastructure.

---

## AI analysis idempotency

An AI analysis is associated with an engineering report.

Before generating another analysis, the application checks whether one already exists.

```text
EngineeringReport
       │
       └── AIAnalysis
```

This prevents unnecessary duplicate analyses and LLM calls.

---

## Synchronization idempotency

GitHub and Jira synchronization use external identifiers to distinguish existing records from new records.

Repeated synchronization therefore follows:

```text
External item
     ↓
Exists?
 ┌───┴────┐
Yes       No
 ↓         ↓
Update   Create
```

---

# 🤖 README automation

Part of this README is automatically maintained.

The script is located at:

```text
scripts/update-readme.sh
```

It:

1. executes the automated test suite;
2. aggregates results from all test projects;
3. updates the generated implementation status section;
4. fails if the test suite fails.

The generated section is delimited by:

```html

<!-- AUTO-GENERATED:START -->

## 🚀 Current implementation status

### Integrations

- ✅ GitHub
- ✅ Jira
- ✅ OpenAI
- 🧪 Fake LLM provider

### Engineering Metrics

| Metric | Source |
|---|---|
| Cycle Time | GitHub |
| PR Review Time | GitHub |
| Deployment Frequency | GitHub |
| Change Failure Rate | GitHub |
| Lead Time | Jira |
| Open PRs | GitHub |
| Merged PRs | GitHub |
| Blocked Items | Jira |

### Automated tests

```text
361 tests
361 passed
0 failed
0 skipped
```

<!-- AUTO-GENERATED:END -->
```

A GitHub Actions workflow can execute this script after changes on `main` and commit the README when generated information changes.

---

# 🧭 Roadmap

## Phase 1 — Engineering intelligence foundation

- [x] Team management
- [x] Team members
- [x] Engineering metrics
- [x] Engineering health score
- [x] Engineering insights
- [x] Risk detection
- [x] Recommended actions
- [x] Engineering reports
- [x] Report persistence
- [x] API endpoints
- [x] Unit tests
- [x] Integration tests

---

## Phase 2 — AI Engineering Manager

- [x] `ILlmProvider`
- [x] Fake LLM provider
- [x] OpenAI provider
- [x] AI report analysis
- [x] AI insights
- [x] AI actions
- [x] Duplicate analysis prevention
- [x] Structured LLM output parsing
- [ ] Prompt versioning / improved prompt management
- [ ] AI confidence and evidence
- [ ] Analysis history
- [ ] AI cost tracking

---

## Phase 3 — GitHub engineering data

- [x] GitHub connection
- [x] Connection validation
- [x] Repository synchronization
- [x] Pull request synchronization
- [x] Pull request review synchronization
- [x] Deployment synchronization
- [x] Cycle Time from GitHub
- [x] PR Review Time from GitHub
- [x] Deployment Frequency from GitHub
- [x] Change Failure Rate from GitHub
- [x] Open PRs from GitHub
- [x] Merged PRs from GitHub
- [ ] GitHub pagination beyond current synchronization limits where applicable
- [ ] Additional GitHub engineering signals

---

## Phase 4 — Jira engineering data

- [x] Jira connection
- [x] Project key configuration
- [x] Connection validation
- [x] Jira issue search
- [x] Jira pagination
- [x] Jira work item persistence
- [x] Jira synchronization
- [x] Blocked Items from Jira
- [x] Lead Time from Jira
- [ ] Exact Done transition using Jira changelog
- [ ] Sprint synchronization
- [ ] Additional workflow/status analytics

---

## Phase 5 — Engineering Management cockpit

- [ ] Web dashboard
- [ ] Team health overview
- [ ] Metric trends
- [ ] Risk dashboard
- [ ] Action tracking
- [ ] Historical comparisons
- [ ] Team-level filtering
- [ ] Engineering health evolution
- [ ] Weekly engineering brief

---

## Phase 6 — Additional integrations

Future integrations may include:

- [ ] GitLab
- [ ] Linear
- [ ] Azure DevOps
- [ ] Datadog
- [ ] additional CI/CD providers
- [ ] Slack signals where they provide meaningful engineering context

---

## Phase 7 — Advanced Engineering Manager Copilot

Longer-term capabilities include:

- contextual team analysis;
- recurring engineering reviews;
- automatic detection of delivery anomalies;
- engineering risk forecasting;
- action follow-up;
- post-mortem assistance;
- engineering decision support;
- team health trends;
- 1:1 support;
- team development insights;
- goal tracking;
- incident intelligence;
- technical debt intelligence;
- CTO-level views;
- engineering benchmarks.

---

# 🚧 Current limitations / technical debt

The project is still an MVP.

Known limitations currently include:

### Lead Time

Jira `resolutiondate` is currently used as `DoneAt`.

This is a pragmatic MVP approximation.

Using Jira changelog data would provide the exact transition time into a completed workflow state.

### Lead Time reporting period

The current Lead Time implementation still selects work items based on the current repository period semantics.

A future improvement should evaluate whether Lead Time reporting should select items completed during the reporting period using `DoneAt`.

### Change Failure Rate

The current implementation is based on deployment status and is an approximation of full DORA Change Failure Rate.

A more mature implementation could correlate deployments with incidents, rollbacks or production failures.

### GitHub pagination

Some GitHub synchronization paths currently have bounded API retrieval and should eventually support complete pagination for larger repositories.

### Authentication

The current MVP test/application flow uses a current-user abstraction and a fixed test user in integration testing.

Production-grade authentication and authorization remain future work.

### Dashboard

There is currently no production web dashboard.

The product is backend/API-first at this stage.

### Historical intelligence

Historical trends and cross-period comparisons remain limited.

### CI integration-test initialization

Integration tests use a shared PostgreSQL test database. CI initialization is being hardened to avoid concurrent migration/seed operations when multiple test fixtures initialize the same database.

---

# 🧠 Product principles

## Evidence before opinion

Recommendations should be grounded in observable engineering data.

---

## Fewer, better actions

The goal is not to generate hundreds of recommendations.

The system should identify the few actions that matter most.

---

## AI as a copilot

AI supports Engineering Manager decision-making.

It does not replace engineering leadership.

---

## Explainability

Insights, risks and AI recommendations should be traceable back to engineering signals.

---

## Human ownership

Engineering decisions remain under human ownership.

---

## Provider independence

The core application should not become dependent on GitHub, Jira or a specific AI provider.

---

## Incremental delivery

The project favors:

```text
Working software
      >
Architecture perfection
```

while maintaining:

```text
Coherent architecture
      >
Quick hacks that become impossible to maintain
```

---

# 📈 Quality goals

The project aims to maintain:

```text
Build         → 0 errors
Tests         → 100% green
Architecture  → Clear boundaries
Domain        → Infrastructure independent
Integrations  → Behind abstractions
AI            → Provider independent
Secrets       → Never committed
Delivery      → Small incremental changes
```

---

# 📄 License

License to be defined.

<!-- AUTO-GENERATED:START -->

## 🚀 Current implementation status

### Integrations

- ✅ GitHub
- ✅ Jira
- ✅ OpenAI
- 🧪 Fake LLM provider

### Engineering Metrics

| Metric | Source |
|---|---|
| Cycle Time | GitHub |
| PR Review Time | GitHub |
| Deployment Frequency | GitHub |
| Change Failure Rate | GitHub |
| Lead Time | Jira |
| Open PRs | GitHub |
| Merged PRs | GitHub |
| Blocked Items | Jira |

### Automated tests

```text
282 tests
282 passed
0 failed
0 skipped
```

<!-- AUTO-GENERATED:END -->
