# First-run installation and local administrator authentication

This first authentication increment installs exactly one **local platform
administrator**. There is no public signup, default password, automatic
promotion of an existing user, or external-user login. The next increment adds
installation-wide Auth0, Okta and Microsoft Entra provider drafts, real OIDC
connection testing and explicit configuration activation; see [SSO configuration](sso.md).
Activation does not yet enable SSO login or provisioning. Local login remains
independent of those provider configurations.

## Prepare the database and installation secret

Keep the existing PostgreSQL connection configuration. Apply the migration
before serving requests:

```sh
dotnet ef database update \
  --project src/AiEngineeringManagerCopilot.Infrastructure \
  --startup-project src/AiEngineeringManagerCopilot.Api
```

Supply `Authentication__SetupSecret` through the API process environment (or
the deployment platform's secret environment injection). Generate at least
32 random bytes; the configured representation must be 43–512 characters:

```sh
export Authentication__SetupSecret="$(openssl rand -base64 32)"
```

There is deliberately no default. Do not put its value in appsettings, source
control, shell history, screenshots, request logs, or frontend configuration.
Do not reuse a memorable phrase. Without a valid configured secret,
installation is unavailable. Supply the generated secret only to the operator.

Start the API and frontend, then visit the frontend `/setup` URL. Enter the
installation secret, a previously unused email, a display name, and a password
of 12–128 characters containing uppercase, lowercase, a digit, a symbol, and
at least four distinct characters. Setup signs in the administrator immediately.
Development's seeded user is not an administrator and is not promoted; use a
different email. Existing seeded teams remain owned by their original user.

Installation and administrator creation commit atomically under a PostgreSQL
transaction-scoped advisory lock. A singleton `Installations` row permanently
records completion independently of the number of users. Concurrent attempts
cannot create two administrators. Subsequent attempts are rejected.

Remove the secret from the process environment after installation and restart
the API. The marker, not the secret or user count, keeps installation closed.
Deleting all users **does not reset installation**. Never delete the marker
to recover an account on a live installation.

## Subsequent login and logout

Visit `/admin/login` for local administrator login. Failed credentials have a
generic response. Five failed password attempts lock the account for 15
minutes; setup/login also share a per-source-IP limit of 10 attempts/minute.
Account lockout updates are serialized using PostgreSQL row locks. Configure
any trusted reverse proxy deliberately: unconfigured proxies share one source
IP and therefore one limiter bucket. Do not trust arbitrary forwarded headers.

Identity Core `PasswordHasher<IdentityUser>` and its password validators handle
password storage/policy. The existing domain `User` and team-owner foreign keys
are unchanged. `LocalAdministrators` stores local credentials and the explicit
`PlatformAdministrator` role, while `AdministratorSessions` references that
credential. Identity's validation-only store does not implement account writes;
all account/session persistence is in these EF tables. There is no role field
in the public setup request, password reset endpoint, or account-management UI.

Accounts with `IsActive = false` cannot login and all their existing sessions
are rejected on the next request. Deleting a user cascades its local credential
and sessions, but not the installation marker. Recovery in this increment is
an operator task: restore an account/database backup, or use a controlled
maintenance procedure producing hashes through ASP.NET Core Identity and
revoking all affected sessions. Never insert plaintext passwords or reset the
installation marker. Self-service recovery will require a later increment.

## Session and deployment security

Sessions use 32 random bytes in an opaque `HttpOnly`, `SameSite=Strict`, host-only
cookie; only a SHA-256 hash is stored in PostgreSQL. Sessions expire absolutely
after eight hours with no sliding extension. A new login rotates the presented
session; logout deletes it server-side and expires the cookie. Expired sessions
are cleaned up on subsequent successful logins. Neither passwords nor session
values are logged by the authentication code; do not enable sensitive EF data
logging or HTTP request/cookie/body logging.

Outside Development/Test, cookies are `Secure`, setup/login/logout require
HTTPS, and session authentication refuses HTTP. Run the API with TLS. If TLS
is terminated at a reverse proxy, explicitly configure validated, trusted
proxy HTTPS forwarding before this middleware (not blanket trust of headers).
For the production Angular build the API base URL is `/api`: deploy a
same-origin `/api` reverse proxy that strips this prefix and connects to the
API over HTTPS, or intentionally change the environment URL for your topology.
Development uses `http://localhost:4200` and `http://localhost:5249`.
SameSite Strict is not intended for unrelated frontend/API sites.

All session-authenticated mutations and authentication mutations require
`X-Session-Protection: 1` **and** an `Origin` matching the API origin or an exact
entry in `Cors:AllowedOrigins`. This includes logout and protects login/setup
against CSRF. The Angular interceptor sends this header and cookie credentials
only to the configured API. CORS allows credentials only for configured
origins—use explicit trusted origins, never a wildcard. Non-browser clients
using sessions must send both headers too. Authentication responses are
`Cache-Control: no-store`.

Existing bearer JWT clients remain supported. When a request contains a Bearer
header, authenticated endpoints validate it rather than silently falling
back to a session cookie. `/auth/current` and `/auth/logout` explicitly require
the local administrator session; a JWT alone cannot impersonate that session.
Existing team ownership checks still apply to the administrator.

The frontend initializes from `/auth/current` without obtaining demo JWTs or
generating dated reports. Unauthenticated protected navigation redirects to
`/setup` only if the server reports setup available, otherwise `/admin/login`.
After login/setup it refreshes accessible teams, clearing stale team selection;
an administrator with no teams starts with no team selected.
The development-only `/dev/token` endpoint is disabled unless the operator
explicitly sets `Development__EnableToken=true`; no normal UI path calls it.

## Endpoints and focused validation

* `GET /auth/setup-status`: only the boolean `setupAvailable`.
* `POST /auth/setup`: installation secret, email, name, password.
* `POST /auth/login`: email and password.
* `GET /auth/current`: the authenticated local administrator profile.
* `POST /auth/logout`: revoke the current server session.

With the repository's PostgreSQL test service running on port 5433:

```sh
dotnet test tests/AiEngineeringManagerCopilot.IntegrationTests \
  --filter FullyQualifiedName~LocalAuthenticationEndpointsTests
cd frontend
npm test -- --watch=false --include='src/app/core/auth/*.spec.ts' \
  --include='src/app/core/app/app-initializer.spec.ts'
npm run build -- --configuration development
```

Authentication integration tests use unique PostgreSQL schemas, real migrations,
and real authentication schemes rather than the existing test identity bypass.
They drop only their own schemas afterward.
