# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.2.0.

## Architecture

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
All these screens still use the shared `public/i18n/sso` namespace; splitting
translation resources is deferred to LOT6.

Shared team DTOs and request models live in `src/app/domains/teams/models`.
The single `TeamApi` in `domains/teams/data-access` owns team and member HTTP
endpoints, including unpaged lists and paginated queries. Import these through
`@domains/teams/*` from both features and core. Team selection state and the
selector remain in `core/team`; team pages and the metrics API remain in
`features/team`.

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
