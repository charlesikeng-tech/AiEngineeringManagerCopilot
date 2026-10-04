# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.0.

## Architecture

```text
src/app/
├── core/
│   ├── app/                  # Startup/session initialization
│   ├── auth/                 # Global session state, interceptor and guards
│   ├── i18n/                 # Global configuration, service and localized pipes
│   ├── layout/               # Application shell and account menu
│   └── team/                 # TeamContext selection state
├── shared/
│   ├── pagination/           # PagedResult and PaginationState
│   └── ui/
│       ├── table-pagination/
│       ├── language-selector/
│       └── team-selector/
├── domains/
│   ├── teams/                # Team contracts and canonical TeamApi
│   └── engineering/          # Cross-feature engineering contracts
└── features/
    ├── authentication/       # Public login and local administrator access
    ├── account/              # Security and explicit identity linking
    ├── administration/       # Installation-wide provider configuration
    ├── team/                 # Team/member management and engineering data
    ├── integrations/         # Provider widgets and provider-owned APIs/models
    ├── dashboard/
    ├── reports/
    ├── actions/
    └── risks/
```

Import cross-area code directly through `@core/*`, `@shared/*`, `@domains/*`,
and `@features/*` (configured in `tsconfig.json`); use relative imports within
an owner. No barrels or duplicate contract exports are needed. Reusable
controls and pagination contracts live in `shared`, while application-scoped
state, guards, i18n configuration/services/pipes, and startup remain in `core`.
Shared controls keep their selectors, templates, styles, and co-located tests.
The language and team selectors consume the existing `I18nService` and
`TeamContext`; neither singleton is duplicated or renamed.

`core/auth` owns only global authentication state and session HTTP operations
(`Auth`), the session interceptor, and route guards, with their tests.
Login and shared setup/local-administrator access pages belong to
`features/authentication/pages`; their unchanged shared page styles live in
`features/authentication/styles`. Public provider discovery and login navigation
belong to `features/authentication/data-access/public-sso-api`.

Provider settings belong to `features/administration/pages/authentication-settings`
and `features/administration/data-access/sso-api`. Account security and identity
linking belong to `features/account/pages/security` and
`features/account/data-access/account-link-api`. Account code uses the public
authentication API through `@features/authentication/data-access/public-sso-api`;
imports within each feature are short relative paths. Tests follow their owners.
Route URLs, guard ordering, templates, and API contracts remain unchanged.

Shared team DTOs and request models live in `src/app/domains/teams/models`.
The single `TeamApi` in `domains/teams/data-access` owns team and member HTTP
endpoints, including unpaged lists and paginated queries. Import these through
`@domains/teams/*` from features, core, and shared controls. Team selection state
remains in `core/team`; its reusable selector lives in `shared/ui/team-selector`.
Team pages and the metrics API remain in `features/team`.

The team route owns team loading, editing, and deletion. `components/team-members`
owns member paging and deletion, with `team-member-editor` owning its drawer,
validation, and saves. `team-engineering-data` owns calculation periods, metrics,
and report generation. Children receive the loaded team ID and selection version
so they cannot act on a stale route selection.

`IntegrationSettings` only composes the team selector and source/notification
sections. GitHub, Slack, and Microsoft Teams widgets own their provider forms,
connection loading, and actions; Jira remains independent. Each widget provides
its own `IntegrationRequestScope` to cancel requests and reject stale callbacks,
including same-tick A → B → A selection changes. Shared widget styles are
feature-local SCSS partials, compiled in each child's encapsulated scope.

Connector models, HTTP APIs, and provider-specific validators live in
`features/integrations/providers/{github,jira,slack,microsoft-teams}`, organized
into `models`, `data-access`, and `validators` as applicable. Jira follows the
same provider ownership pattern as the other connectors. Import these through
`@features/integrations/providers/*` outside the provider; use short relative
imports within a provider. Integration UI components and pages remain in
`features/integrations`.

Shared engineering contracts live in `src/app/domains/engineering/models`,
organized by report, action, risk, metric, health, and AI analysis. Import these
directly through `@domains/engineering/models/*` from features and core; team
metrics use the canonical metric contracts from this domain. The dashboard
response remains a feature-local aggregate, as do feature-owned paged responses
and report history models.

## Translation resources

Every directory under `public/i18n/` contains `en.json` and `fr.json`:

| Resource | Responsibility / keys |
|---|---|
| `common/` | Shared navigation, language, formatting, statuses and metric labels |
| `team/` | Team/member management and team connection forms |
| `dashboard/` | Dashboard labels, charts and tooltips |
| `management/` | Reports, actions and risks |
| `authentication/` | Public SSO login (`sso.login.*`) |
| `account/` | Account security and identity linking (`accountLink.*`) |
| `administration/` | Provider configuration (`sso.*` except `sso.login.*`) |

Resource ownership follows feature responsibility, not necessarily the top-level
translation namespace. The historical `sso` keys are retained to preserve text
and template contracts, but there is no catch-all SSO resource. Authentication
and administration own disjoint leaf keys under `sso`. Shell menus can consume
authentication, account, and administration labels without owning their resources.

`core/i18n/i18n.providers.ts` loads all seven small resources at startup through
the ngx-translate multi-resource HTTP loader, which **deep-merges** shared
namespaces. No route-level lazy translation loading is needed. Missing resource
files fail loading; English remains the missing-key fallback. Test fixtures
assemble nested namespaces explicitly and `provideI18nTesting` deep-merges
overrides with common defaults. The i18n specs verify loading, language parity,
nonoverlapping ownership, and preservation of all original split keys/text.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
