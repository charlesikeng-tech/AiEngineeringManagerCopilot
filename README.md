# AI Engineering Manager Copilot

AI Engineering Manager Copilot is an engineering intelligence platform
designed to help Engineering Managers understand team health, identify
delivery and engineering risks, and turn engineering data into concrete
actions.

The goal is not to replace engineering leadership.

The product acts as an evidence-based copilot that helps Engineering
Managers:

-   understand engineering health;
-   detect delivery and quality risks;
-   identify bottlenecks;
-   monitor engineering metrics;
-   connect GitHub and Jira engineering data;
-   generate actionable recommendations;
-   support engineering decisions;
-   reduce the time spent collecting and interpreting engineering
    information.

------------------------------------------------------------------------

## 🎯 Vision

Engineering Managers have access to large amounts of engineering data
across tools such as GitHub, Jira and CI/CD platforms.

The difficult part is not collecting data.

The difficult part is turning that data into a coherent understanding
of:

-   what is happening;
-   why it matters;
-   what is becoming risky;
-   what should be done next.

AI Engineering Manager Copilot aims to create the following continuous
loop:

``` text
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

> What are the three most important things an Engineering Manager should
> do this week?

The answer should be data-driven, contextual, explainable, actionable
and verifiable.

------------------------------------------------------------------------

# 🚀 Current capabilities

The current backend MVP already supports:

-   team management;
-   team members;
-   GitHub connection management;
-   GitHub repository synchronization;
-   GitHub pull request synchronization;
-   GitHub pull request review synchronization;
-   GitHub deployment synchronization;
-   Jira connection management;
-   Jira connection validation;
-   Jira issue synchronization;
-   Slack report notifications through per-team Incoming Webhooks;
-   engineering metric calculation;
-   engineering health scoring;
-   engineering reports;
-   engineering insights;
-   engineering risk detection;
-   recommended engineering actions;
-   AI-based report analysis;
-   OpenAI integration;
-   deterministic fake LLM execution;
-   PostgreSQL persistence;
-   metric trends and cross-period engineering signal analysis;
-   Early Warning v1 with recommended actions and AI-assisted analysis;
-   resilient GitHub synchronization with pagination, retry and
    rate-limit handling;
-   resilient Jira synchronization with retry and rate-limit handling;
-   background synchronization jobs for GitHub and Jira;
-   current-user abstraction and owner-based team isolation;
-   JWT authentication and endpoint-level authorization;
-   Swagger/OpenAPI Bearer authentication for local development;
-   development-only JWT token generation and deterministic development seeding;
-   deterministic test authentication for integration tests;
-   automated unit and integration testing.

An Angular frontend is also available under `frontend/`. It includes a
dashboard, report list and detail views, risks, action tracking, team
selection, and team/GitHub connection management.

## Production readiness

The application is an MVP with a local development frontend, not a
production-ready deployment. The following gaps were identified in the
2026-10-02 static code audit and remain open:

| Area | Current limitation | Required hardening |
|---|---|---|
| Frontend startup | Startup always uses `/dev/token`, selects the first team, and prepares August/September 2026 reports. The production API URL is still `http://localhost:5249`. | Separate demo initialization from normal startup and configure production authentication/API access. |
| AI request limits | Analysis generation is limited to 5 requests per minute per authenticated user, per API instance; excess requests receive HTTP 429 and a `Retry-After` header. The limit is configurable with `RateLimiting:AIAnalysis:PermitLimit`. | Use a shared/distributed limiter if the deployment runs multiple API instances. |
| Concurrent AI analysis | Existing analyses are checked before generation, but `ReportId` has no unique constraint. | Prevent concurrent duplicate generation and persistence. |
| Historical reports | Stored scores and conclusions are combined with metrics that can later be recalculated. | Snapshot or version the report inputs. |
| GitHub synchronization | Some HTTP failures are treated as empty results, and `LastSyncAt` is set before collection finishes. | Expose partial failures and preserve previously known deployment statuses. |
| Deployment metrics | Unknown statuses enter the failure-rate denominator; later status updates can change the recorded deployment date. | Define explicit status and reporting-period semantics. |
| Frontend state | Report/risk streams terminate after an HTTP error; dashboard cancellation can reset loading for a newer request. | Recover within each request and associate state with the active request. |
| AI output validation | Local parsing does not validate every required field, collection, or enum value. | Reject incomplete or invalid results before persistence. |
| Secret-protection keys | Data Protection is registered without an explicit persistent/shared key store. | Define key persistence and sharing for the deployment topology. |

These are known limitations, not completed fixes. Production readiness
also requires frontend accessibility improvements and pull-request
validation covering both backend and frontend.

------------------------------------------------------------------------

# 🔌 Engineering data integrations

## GitHub

GitHub is currently used as an engineering data source.

The application can synchronize:

``` text
GitHub
 │
 ├── Repositories
 ├── Pull Requests
 ├── Pull Request Reviews
 └── Deployments
```

The synchronization flow is:

``` text
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

GitHub credentials are stored through the connection abstraction and the
access token is protected before persistence.

### GitHub metrics

GitHub currently provides the source data for:

Metric                 GitHub data
  ---------------------- -------------------------
Cycle Time             Pull Requests
PR Review Time         Pull Requests + Reviews
Deployment Frequency   Deployments
Change Failure Rate    Deployments
Open PRs               Pull Requests
Merged PRs             Pull Requests

------------------------------------------------------------------------

## Jira

Jira is also implemented as an engineering data source.

A team can configure a Jira connection using:

-   Jira base URL;
-   account email;
-   API token;
-   project key.

The application can validate the connection before synchronization.

The synchronization flow is:

``` text
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

``` text
ExternalId
Key
Summary
Status
AssigneeExternalId
CreatedAt
DoneAt
IsBlocked
```

Issue synchronization supports pagination through Jira's enhanced JQL
search API.

For the current MVP:

``` text
Jira resolutiondate
        ↓
JiraIssue.DoneAt
        ↓
JiraWorkItem.DoneAt
```

`resolutiondate` is therefore currently used as the completion
timestamp.

A future improvement can use Jira changelog information to determine the
exact workflow transition into a completed state.

### Jira metrics

Jira currently provides the source data for:

Metric          Jira data
  --------------- -----------------------------------
Lead Time       Work item CreatedAt → DoneAt
Blocked Items   Jira work items marked as blocked

------------------------------------------------------------------------

# 📊 Engineering metrics

The platform currently supports eight engineering metrics.

  --------------------------------------------------------------------------
Metric                Source   Current meaning
  --------------------- -------- -------------------------------------------
Cycle Time            GitHub   Average duration of merged pull requests

PR Review Time        GitHub   Time between PR creation and first review

Deployment Frequency  GitHub   Successful deployments during the period

Change Failure Rate   GitHub   Failed deployments / total deployments

Lead Time             Jira     Work item creation → completion

Open PRs              GitHub   Open pull requests

Merged PRs            GitHub   Merged pull requests

Blocked Items         Jira     Blocked Jira work items
--------------------------------------------------------------------------

Metrics are persisted as `EngineeringMetric` records and associated
with:

``` text
Team
MetricType
Value
PeriodStart
PeriodEnd
CreatedAt
```

The metric architecture is intentionally extensible so additional
signals can be introduced later.

`EngineeringMetricsService` uses the same calculation pipeline for
individual metrics and the complete set. A complete calculation checks
team ownership once, reads each integration connection once, and shares
period pull requests and deployments between the calculators that need
them. Input data is loaded sequentially into a per-call context; it is
not cached across teams, periods, or requests.

Individual calculations only load the source and datasets they need.
The distinction between `SourceNotConfigured`, `NoData`, and an available
zero value is preserved.

------------------------------------------------------------------------

# ❤️ Engineering health

Metrics feed an engineering health scoring layer.

``` text
Engineering Metrics
        ↓
Health Score
        ↓
Health Level
```

The current health levels are:

``` text
Excellent
Healthy
Needs Attention
At Risk
Critical
```

The health score provides a summarized view of engineering conditions
for a selected reporting period.

------------------------------------------------------------------------

# 📄 Engineering reports

The application can generate engineering reports for a team and a
selected period.

A report contains:

-   reporting period;
-   overall engineering health score;
-   health level;
-   executive summary;
-   engineering insights;
-   detected risks;
-   recommended actions.

Example:

``` text
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

------------------------------------------------------------------------

# ⚠️ Risk detection

The application contains a rule-based engineering risk engine.

It can detect signals such as:

``` text
High change failure rate
Low deployment frequency
High cycle time
High PR review time
High lead time
High number of blocked items
```

An `EngineeringRisk` contains information such as:

``` text
Category
Title
Description
Severity
```

Supported severity levels include:

``` text
Critical
High
Medium
Low
```

The rules are intentionally deterministic and explainable.

AI analysis is an additional layer and does not replace deterministic
engineering rules.

------------------------------------------------------------------------

# 🎯 Engineering actions

Engineering reports can generate concrete actions from detected
engineering conditions.

An `EngineeringAction` contains information such as:

``` text
Title
Description
Priority
Status
Report association
```

The report generation process intentionally limits the number of
recommended actions.

The objective is:

> Fewer, better actions.

The product should help an Engineering Manager identify the most
important interventions rather than generate another large backlog.

------------------------------------------------------------------------

# 🤖 AI Engineering Analysis

The application contains an AI analysis layer built behind an
abstraction.

``` csharp
public interface ILlmProvider
{
    Task<LlmAnalysisResult> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken);
}
```

The application therefore does not directly depend on a specific LLM
implementation.

The AI receives structured engineering context including:

-   reporting period;
-   overall score;
-   executive summary;
-   engineering metrics;
-   existing insights;
-   detected risks.

It can produce:

-   executive analysis;
-   additional insights;
-   recommended actions;
-   action priorities.

The AI layer uses structured output parsing so application behavior
remains controlled by the application rather than by free-form LLM text.

------------------------------------------------------------------------

# 🧪 Fake LLM provider

A deterministic `FakeLlmProvider` is available.

It allows the complete AI workflow to be tested without:

-   an OpenAI API key;
-   network access;
-   API costs;
-   non-deterministic model responses.

This provider is used extensively by automated tests.

``` text
Application
     ↓
 ILlmProvider
   ↙     ↘
Fake    OpenAI
```

------------------------------------------------------------------------

# 🔐 OpenAI integration

A real OpenAI implementation is available behind `ILlmProvider`.

The implementation uses the official OpenAI .NET package.

The OpenAI integration handles configuration and validates conditions
such as:

-   missing API key;
-   missing/invalid model configuration;
-   empty model responses;
-   invalid structured responses.

OpenAI is optional.

The application can run locally and execute its automated tests using
the fake provider.

Never commit an OpenAI API key to Git.

------------------------------------------------------------------------

# 🏗️ Architecture

The backend is implemented as a pragmatic modular monolith using Clean
Architecture principles.

``` text
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

------------------------------------------------------------------------

## API startup

`Program.cs` is the API composition root. Service registration is grouped
in `AiEngineeringManagerCopilot.Api/DependencyInjection/` by responsibility:
API configuration, authentication, persistence, application services,
GitHub/Jira integrations, AI analysis, and background jobs.

Middleware ordering, endpoint mapping, development initialization, and
health routes remain explicit in `Program.cs`. The registration modules
preserve service lifetimes, configuration validation, and the existing
Development, Test, and Production behavior.

The registration modules are:

| Module | Responsibility |
|---|---|
| `ApiServiceCollectionExtensions` | JSON, health checks, OpenAPI, CORS, and rate-limit policy registration |
| `AuthenticationServiceCollectionExtensions` | JWT options, authentication, authorization, and current-user resolution |
| `PersistenceServiceCollectionExtensions` | PostgreSQL, repositories, secret protection, and development initialization |
| `ApplicationServiceCollectionExtensions` | Validators, use cases, metric calculators, and engineering intelligence |
| `IntegrationServiceCollectionExtensions` | GitHub/Jira services and HTTP clients |
| `AIServiceCollectionExtensions` | LLM configuration, providers, parsing, and analysis services |
| `BackgroundJobServiceCollectionExtensions` | Synchronization runners and environment-specific hosted services |

Registration coverage in
`tests/AiEngineeringManagerCopilot.UnitTests/DependencyInjection/ServiceRegistrationTests.cs`
checks service resolution, lifetimes, environment-specific registrations,
authentication/API configuration, and invalid startup options.

## Dependency direction

The project dependencies follow:

``` text
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

-   Entity Framework Core;
-   PostgreSQL;
-   GitHub;
-   Jira;
-   OpenAI;
-   ASP.NET Core.

External systems are accessed through abstractions.

------------------------------------------------------------------------

# 🧩 Main application areas

The current application is organized around several capabilities.

``` text
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
├── Metric Trends
├── Early Warning
└── AI
```

Infrastructure provides the implementations required by those
application capabilities.

------------------------------------------------------------------------

# 🗃️ Domain model

The current backend contains domain entities including:

``` text
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

This separates external engineering data from the intelligence generated
from that data.

------------------------------------------------------------------------

# 🗄️ Persistence

The application uses:

``` text
Entity Framework Core
        ↓
PostgreSQL
```

`AppDbContext` persists the main application entities.

Database schema changes are managed using EF Core migrations.

Examples of persisted engineering data include:

``` text
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

Uniqueness constraints are used for external synchronized data to
prevent duplicate records during repeated synchronization.

Examples include repository/external identifiers and Jira team/external
issue identifiers.

------------------------------------------------------------------------

# 🔄 Synchronization strategy

GitHub and Jira synchronization are designed to be repeatable.

The general model is:

``` text
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

This allows synchronization to update existing data instead of blindly
creating duplicates.

------------------------------------------------------------------------

# 🌐 API

The application exposes ASP.NET Core Minimal APIs.

The main API areas currently include:

``` text
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

``` http
/teams/{teamId}/metrics
```

Available metric operations include:

``` http
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

``` text
periodStart
periodEnd
```

------------------------------------------------------------------------

## Engineering reports

``` http
POST /teams/{teamId}/reports
```

Generate an engineering report.

``` http
GET /teams/{teamId}/reports
```

Retrieve reports belonging to the team.

``` http
GET /teams/{teamId}/reports/{reportId}
```

Retrieve a specific report.

``` http
POST /teams/{teamId}/reports/{reportId}/analyze
```

Generate or retrieve an AI analysis for a report.

------------------------------------------------------------------------

## GitHub integration

GitHub endpoints allow a team to:

``` text
Configure connection
Retrieve connection
Delete connection
Test connection
Synchronize engineering data
```

The GitHub synchronization pipeline covers:

``` text
Repositories
Pull Requests
Pull Request Reviews
Deployments
```

------------------------------------------------------------------------

## Jira integration

Jira endpoints are grouped under:

``` http
/teams/{teamId}/jira
```

Current operations include:

``` http
POST /teams/{teamId}/jira
GET /teams/{teamId}/jira
DELETE /teams/{teamId}/jira
POST /teams/{teamId}/jira/test
POST /teams/{teamId}/jira/sync
```

The Jira synchronization endpoint imports Jira issues into
`JiraWorkItem`.

------------------------------------------------------------------------

## Slack report notifications

Each team can configure a Slack Incoming Webhook from its team settings.
The webhook is encrypted before it is stored, is never returned by the API,
and is restricted to HTTPS URLs hosted on `hooks.slack.com`. Slack receives
a message after a report is successfully persisted, containing the team,
reporting period, health score, and health level. A Slack delivery failure is
logged and does not roll back report generation.

The **Send test** action posts a test message to the configured channel.
Remove the webhook from team settings to stop notifications. Apply the
`AddSlackWebhookConnection` EF migration before deploying the feature:

```sh
dotnet ef database update \
  --project src/AiEngineeringManagerCopilot.Infrastructure \
  --startup-project src/AiEngineeringManagerCopilot.Api
```

------------------------------------------------------------------------

# 🛠️ Technology stack

## Backend

-   .NET 10
-   C#
-   ASP.NET Core Minimal APIs
-   Entity Framework Core
-   PostgreSQL

## External integrations

-   GitHub REST API
-   Jira Cloud REST API
-   Slack Incoming Webhooks
-   OpenAI

## Frontend

-   Angular 22 and TypeScript
-   RxJS and Angular signals
-   NG-ZORRO Ant Design
-   ECharts / ngx-echarts
-   Angular CLI unit tests with Vitest

## Testing

-   xUnit
-   FluentAssertions
-   `WebApplicationFactory`
-   PostgreSQL integration tests
-   Fake GitHub client
-   Fake Jira client
-   Fake LLM provider

## API

-   Minimal APIs
-   OpenAPI / Swagger

## Development

-   .NET CLI
-   Git
-   Docker / Docker Compose
-   Visual Studio Code / Rider
-   macOS / Linux-compatible CI

## CI/CD

-   GitHub Actions

------------------------------------------------------------------------

# 🧪 Testing

The solution contains two main automated test projects:

``` text
AiEngineeringManagerCopilot.UnitTests
AiEngineeringManagerCopilot.IntegrationTests
```

The backend suite includes both unit and integration tests. Counts evolve
as scenarios are added; the generated README status section records the
last results produced by `scripts/update-readme.sh`, not a live guarantee
for the current worktree. Frontend tests are separate and are not included
in that generated total.

Run all tests:

``` bash
dotnet test
```

The automated suite covers areas including:

-   team management;
-   team members;
-   repository persistence;
-   GitHub connection management;
-   GitHub synchronization;
-   pull request synchronization;
-   review synchronization;
-   deployment synchronization;
-   Jira connection management;
-   Jira connection validation;
-   Jira issue synchronization;
-   metric calculators;
-   metric endpoints;
-   engineering health;
-   engineering reports;
-   risk generation;
-   action generation;
-   AI analysis;
-   structured LLM parsing;
-   reuse of an existing AI analysis on sequential requests;
-   fake and real-provider configuration behavior;
-   metric trends;
-   Early Warning detection and actions;
-   GitHub pagination, retry and rate-limit resilience;
-   Jira retry and rate-limit resilience;
-   GitHub and Jira background synchronization;
-   current-user behavior and team ownership isolation;
-   JWT/test-authentication infrastructure;
-   dependency-injection registration and environment-specific behavior.

Existing-analysis reuse does not currently guarantee idempotency for
concurrent requests. Frontend coverage also needs strengthening: the
starter app-title assertion is outdated, and the interceptor test does
not exercise an HTTP request.

Integration tests use PostgreSQL.

The test environment currently uses:

``` text
Database: ai_engineering_manager_test
Port:     5433
```

------------------------------------------------------------------------

# 💻 Local development

## Prerequisites

Install:

-   .NET 10 SDK;
-   PostgreSQL or Docker;
-   Git.

Verify .NET:

``` bash
dotnet --version
```

Restore:

``` bash
dotnet restore
```

Build:

``` bash
dotnet build
```

Run tests:

``` bash
dotnet test
```

For a local database, start the development PostgreSQL service:

``` bash
docker compose up -d postgres
```

Supply `ConnectionStrings:Default` and `Jwt:Key` through User Secrets or
environment variables before starting the API. The JWT signing key must
contain at least 32 characters. The Development configuration currently
selects OpenAI; set `Llm:Provider` to `Fake` for local execution without an
OpenAI API key.

For example, configure the fake provider with:

``` bash
dotnet user-secrets set "Llm:Provider" "Fake" --project src/AiEngineeringManagerCopilot.Api
```

Run the API with the HTTP development profile:

``` bash
dotnet run --project src/AiEngineeringManagerCopilot.Api --launch-profile http
```

The API is available at `http://localhost:5249`; Swagger is available at
`http://localhost:5249/swagger`.

## Frontend development

Frontend imports use aliases defined in `frontend/tsconfig.json`:

| Alias | Directory |
|---|---|
| `@core/*` | `frontend/src/app/core/*` |
| `@features/*` | `frontend/src/app/features/*` |
| `@environments/*` | `frontend/src/environments/*` |

Use aliases for imports across application areas and relative imports
within the same feature. Both application and test TypeScript
configurations inherit these aliases.

Install a Node.js version supported by Angular 22 and npm. From a separate
terminal, with the Development API running:

``` bash
cd frontend
npm ci
npm start
```

Open `http://localhost:4200`. The current frontend startup requests a
development JWT and prepares demo reports; it must not be used unchanged
as a production authentication flow.

Frontend commands, run from `frontend/`:

``` bash
npm run build
npm test -- --watch=false
```

### Frontend asset loading

The application keeps NG-ZORRO's base styles, animations, and CDK overlay
styles, but imports component styles selectively through
`frontend/src/styles/ng-zorro.css` instead of the complete library
stylesheet. Add the appropriate component stylesheet there when adding
a new NG-ZORRO component; precompiled entry styles include their library
dependencies.

ECharts is loaded only when the dashboard health chart is instantiated.
The module under `features/dashboard/echarts/` registers the line chart,
axes, tooltip, and canvas renderer; it is not part of the initial bundle.

Production size budgets remain unchanged: 1.5 MB warning / 2 MB error
for initial assets, and 8 kB warning / 12 kB error per component stylesheet.

The 2026-10-02 production asset measurements are:

| Asset | Before optimization | After optimization |
|---|---|---|
| Initial assets, raw | 1.57 MB | 767.42 kB |
| Initial assets, estimated transfer | 333.55 kB | 151.41 kB |
| Global stylesheet, raw | 564.76 kB | 300.40 kB |
| Report detail stylesheet | 12.90 kB | 11.65 kB |
| Team detail stylesheet | 10.99 kB | 10.53 kB |

The report stylesheet is below the blocking 12 kB limit. Report and team
stylesheets still exceed the 8 kB warning threshold; further reduction
remains possible without relaxing the budgets. The deferred ECharts
chunk is approximately 499 kB raw and is additional to the initial assets
when the chart is displayed.

### Frontend internationalization

The interface supports **English and French** with an in-app language
selector in the header. Switching languages does not reload the page or
change the selected team, form values, API enum values, or stored
engineering data.

The saved preference (`em-copilot.language` in browser local storage) takes
precedence over browser language detection. French browser locales select
French; other browser locales use English. The selection synchronizes
document language/title, NG-ZORRO controls, date-fns calendar formatting,
and localized date, number, and percentage display.

Runtime translation dictionaries live under `frontend/public/i18n/`:

| Directory | Content |
|---|---|
| `common/` | Navigation, language selection, metric names, statuses, and health levels |
| `team/` | Team management, members, and GitHub connection forms |
| `dashboard/` | Dashboard components, chart labels, and tooltips |
| `management/` | Reports, actions, and risks |

Each directory contains `en.json` and `fr.json`. `@ngx-translate/core`
loads the selected language through the HTTP loader configured in
`core/i18n/i18n.providers.ts`. English is the fallback for missing keys;
a missing dictionary file is surfaced as a load error instead of silently
serving partial translations. Deploy these JSON assets with the frontend.

Use `TranslatePipe` for template text and `I18nService.t()` for
imperative messages and derived labels. Use `appDate`, `appNumber`, and
`appPercent` for displays that must react to language changes without
changing the underlying value. Keep English API identifiers separate
from translated display labels.

Backend-generated summaries, insights, recommendations, GitHub/Jira
content, and user-entered names/descriptions are not translated by this
frontend layer.

------------------------------------------------------------------------

# ⚙️ Configuration

The application supports selecting the LLM provider through
configuration.

Example:

``` json
{
  "Llm": {
    "Provider": "Fake",
    "ApiKey": "",
    "Model": ""
  }
}
```

For local development:

``` text
Provider = Fake
```

This allows AI workflows to execute without an external API.

For OpenAI:

``` text
Provider = OpenAI
ApiKey   = <secret>
Model    = <model>
```

## JWT authentication

JWT bearer authentication infrastructure is configured in the API.

Example configuration:

``` json
{
  "Jwt": {
    "Issuer": "AiEngineeringManagerCopilot",
    "Audience": "AiEngineeringManagerCopilot",
    "Key": ""
  }
}
```

The signing key must be supplied through User Secrets or environment
configuration and must never be committed to Git.

JWT validation, HTTP current-user resolution, owner-based team isolation and
endpoint-level authorization are implemented across the protected team API
areas. In Development, Swagger supports Bearer authentication and a
development-only token endpoint can be used to exercise the authenticated API
end to end.

------------------------------------------------------------------------

# 🔒 Secrets

Secrets must never be committed to Git.

Sensitive integration values include:

``` text
OpenAI API key
GitHub access token
Jira API token
JWT signing key
```

For local .NET configuration, User Secrets or environment variables
should be used.

Example:

``` bash
dotnet user-secrets set "Llm:ApiKey" "YOUR_API_KEY"
```

Local secret/configuration files should remain outside source control.

------------------------------------------------------------------------

# 📁 Project structure

``` text
AiEngineeringManagerCopilot/
│
├── src/
│   │
│   ├── AiEngineeringManagerCopilot.Api/
│   │   ├── DependencyInjection/
│   │   ├── Endpoints/
│   │   └── Program.cs
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
├── frontend/
│   ├── src/app/core/
│   ├── src/app/features/
│   └── src/environments/
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

------------------------------------------------------------------------

# 🔑 Key design decisions

## Modular monolith

The MVP remains a modular monolith.

No microservices are currently required.

This keeps deployment and development simple while preserving clear
boundaries between application capabilities.

------------------------------------------------------------------------

## External systems behind abstractions

The application does not expose external provider implementation details
to the domain.

Examples include:

``` text
IGitHubClient
IJiraClient
ILlmProvider
```

This allows:

-   fake implementations in tests;
-   provider replacement;
-   lower coupling;
-   deterministic local execution.

------------------------------------------------------------------------

## Repository abstractions

Application services access persistence through repository abstractions
rather than directly using `AppDbContext`.

Examples include:

``` text
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

------------------------------------------------------------------------

## AI analysis idempotency

An AI analysis is associated with an engineering report.

Before generating another analysis, the application checks whether one
already exists.

``` text
EngineeringReport
       │
       └── AIAnalysis
```

This avoids duplicate analyses and LLM calls on sequential requests.
It does not protect simultaneous requests: both can pass the existence
check, and the database currently has no unique constraint on `ReportId`.
Concurrency-safe generation and persistence remain required.

------------------------------------------------------------------------

## Synchronization idempotency

GitHub and Jira synchronization use external identifiers to distinguish
existing records from new records.

Repeated synchronization therefore follows:

``` text
External item
     ↓
Exists?
 ┌───┴────┐
Yes       No
 ↓         ↓
Update   Create
```

------------------------------------------------------------------------

# 🤖 README automation

Part of this README is automatically maintained.

The script is located at:

``` text
scripts/update-readme.sh
```

It:

1.  executes the automated test suite;
2.  aggregates results from all test projects;
3.  updates the generated implementation status section;
4.  fails if the test suite fails.

The generated section is delimited by:

``` html

<!-- AUTO-GENERATED:START -->

## 🚀 Current implementation status

### Integrations

- ✅ GitHub
- ✅ Jira
- ✅ OpenAI
- 🧪 Fake LLM provider

### Engineering intelligence

- ✅ 8 engineering metrics
- ✅ Engineering health
- ✅ Engineering reports
- ✅ Risk detection
- ✅ Recommended actions
- ✅ AI analysis
- ✅ Metric Trends v1
- ✅ Early Warning v1

### Synchronization resilience

- ✅ GitHub pagination
- ✅ GitHub retry / rate-limit handling
- ✅ Jira retry / rate-limit handling
- ✅ GitHub background synchronization
- ✅ Jira background synchronization

### Authentication & Team Isolation

- ✅ Current-user abstraction
- ✅ HTTP current-user resolution
- ✅ Owner-based Team isolation
- ✅ GitHub / Jira isolation
- ✅ JWT authentication infrastructure
- 🚧 Endpoint-level authorization rollout

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
521 tests
521 passed
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
    - [x] GitHub pagination
    - [x] Retry and rate-limit resilience
    - [x] Background synchronization
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
    - [x] Retry and rate-limit resilience
    - [x] Background synchronization
    - [ ] Exact Done transition using Jira changelog
    - [ ] Sprint synchronization
    - [ ] Additional workflow/status analytics

    ---

    ## Engineering intelligence extensions

    - [x] Metric Trends v1
    - [x] Early Warning v1
    - [x] Early Warning recommended actions
    - [x] AI-assisted Early Warning analysis

    ---

    ## Authentication & Team Isolation v1

    - [x] `ICurrentUser` abstraction
    - [x] HTTP current-user resolution
    - [x] Owner-based Team isolation
    - [x] GitHub connection and sync isolation
    - [x] Jira connection and sync isolation
    - [x] JWT bearer authentication infrastructure
    - [x] Integration-test authentication handler
    - [x] Endpoint-level `RequireAuthorization`
    - [x] Explicit `401 Unauthorized` integration coverage
    - [x] Complete authorization rollout across protected API areas
    - [x] Swagger/OpenAPI Bearer authentication
    - [x] Development-only JWT token generation
    - [x] End-to-end JWT authentication and team-isolation validation

    ---

    ## Phase 5 — Engineering Management cockpit

    The following UI capabilities are available in the local development frontend; checked items do not imply production readiness.

    - [x] Local development web dashboard
    - [x] Team health overview
    - [x] Metric trends
    - [x] Risk dashboard
    - [x] Action tracking
    - [x] Initial report-to-report comparisons
    - [x] Team selection
    - [x] Engineering health evolution
    - [ ] Production authentication and deployment configuration
    - [ ] Frontend error recovery, concurrency, and accessibility hardening
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

    Lead Time selects completed work items whose `DoneAt` falls within the reporting period through `GetCompletedByTeamAndPeriodAsync`. The completion timestamp remains the Jira `resolutiondate` approximation described above.

    ### Change Failure Rate

    The current implementation is based on deployment status and is an approximation of full DORA Change Failure Rate. All statuses enter the denominator, while only `failure` enters the numerator; unknown or pending outcomes can therefore lower the reported rate.

    A more mature implementation could correlate deployments with incidents, rollbacks or production failures.

    ### GitHub synchronization resilience

    GitHub pagination, retry and rate-limit handling are implemented. The synchronization workflow can still hide partial HTTP failures as empty collections and mark the connection as synchronized before all data is collected. Partial-success reporting and preservation of known deployment statuses remain open.

    ### Authentication and authorization

    Authentication & Team Isolation v1 is complete. The API includes JWT bearer authentication, `HttpCurrentUser`, owner-based team isolation, endpoint-level `RequireAuthorization`, and explicit `401 Unauthorized` integration coverage across the protected API areas.

    Local Development also supports Swagger Bearer authentication, a development-only JWT token generator, and deterministic development seed data. The technical `/health` endpoint remains anonymous for liveness checks.

    ### Dashboard

    An Angular dashboard and report, risk, action, and team screens are implemented for local development. The frontend still depends on development authentication and demo initialization, and its production API URL points to localhost.

    Error recovery, concurrent UI state updates, accessibility, and frontend regression coverage need hardening before production use.

    ### Historical intelligence

    Metric Trends v1 provides an initial cross-period view. Report metrics are currently reloaded from mutable period data rather than stored as an immutable snapshot, so recalculation can make historical metrics inconsistent with persisted scores and conclusions. Broader historical intelligence, longer-term comparisons and richer forecasting remain future work.

    ### Continuous integration

    The README workflow runs backend tests after pushes to `main`. There is currently no pull-request validation workflow or frontend build/test workflow. Release automation is not a substitute for a pre-merge quality gate.

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

while maintaining:

``` text
Coherent architecture
      >
Quick hacks that become impossible to maintain
```

------------------------------------------------------------------------

# 📈 Quality goals

The project aims to maintain:

``` text
Build         → 0 errors
Tests         → 100% green
Architecture  → Clear boundaries
Domain        → Infrastructure independent
Integrations  → Behind abstractions
AI            → Provider independent
Secrets       → Never committed
Delivery      → Small incremental changes
```

------------------------------------------------------------------------

# 📄 License

License to be defined.
