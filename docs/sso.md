# Installation-wide SSO configuration, connection testing and effective login

This increment adds the platform administrator page `/admin/authentication`.
It saves Auth0, Okta and Microsoft Entra ID drafts, performs **real OIDC
authorization-code + PKCE** tests, and explicitly activates a successfully
tested revision. Activation enables **effective SSO login at `/login`**, using
only that immutable active snapshot. The approved pilot policy permits anyone
authorized by that active provider application to create an ordinary app account
at their **first login (JIT)**. There are no invitations or preapproval checks.
Restrict application assignments/authorization at the provider accordingly.
There is no public password signup, organization scoping, user import or
automatic external administrator linking. Explicit linking for an existing local
administrator requires local password confirmation and fresh provider proof.
Provider configuration is global to this
installation. Deactivation never disables or deletes the local recovery account
at `/admin/login`.

## Operator configuration

Apply the migration before serving the new endpoints:

```sh
dotnet ef database update \
  --project src/AiEngineeringManagerCopilot.Infrastructure \
  --startup-project src/AiEngineeringManagerCopilot.Api
```

Set these **fixed, trusted process environment values**, not request Host or
untrusted forwarded headers:

```sh
# Development only:
export Authentication__Sso__PublicApiBaseUrl=http://localhost:5249
export Authentication__Sso__FrontendOrigin=http://localhost:4200
```

Production must use HTTPS. For the default production same-origin `/api`
reverse proxy, for example:

```sh
export Authentication__Sso__PublicApiBaseUrl=https://copilot.example.com/api
export Authentication__Sso__FrontendOrigin=https://copilot.example.com
```

`PublicApiBaseUrl` may include the proxy base path. `FrontendOrigin` must be
an origin only. No credentials, query or fragment are accepted. HTTP is allowed
only for the literal `localhost` host in Development/Test. The proxy must strip
the API prefix, forward the external HTTPS scheme through **explicitly trusted
proxy configuration**, and preserve callback query parameters. Cookies remain
host-only. Development uses the two localhost ports on the same site. Continue
to configure explicit `Cors__AllowedOrigins__0` etc. for the trusted frontend;
do not use wildcard credentialed CORS or unrelated frontend/API sites.

Register **both** exact redirect URIs in the provider application:

* Development: `http://localhost:5249/auth/sso/callback`
* Example production: `https://copilot.example.com/api/auth/sso/callback`
* Development user login: `http://localhost:5249/auth/sso/login/callback`
* Example production user login: `https://copilot.example.com/api/auth/sso/login/callback`

The administrator page displays both URLs derived from the configured
API base; the user-login callback adds `/login` before `/callback`. Register both
**exactly**, including protocol, port and proxy path, at the provider. This
increment uses a GET callback (`response_mode=query`), not `form_post`. Codes
and encrypted state necessarily traverse that protocol callback; no access or
ID tokens are returned in frontend URLs, responses or stored in the database.
Disable/redact query logging for the callback at both API and proxy layers;
do not enable request/body/cookie logging, EF sensitive data logging, IdentityModel
PII, or verbose authentication logs. Protocol errors exposed to the UI are fixed
codes, never provider error descriptions or claims.

## Provider applications

Supply real tenant/application credentials through the password field in the
administrator page; never place them in repository files or frontend environment
configuration.

* **Auth0:** create a Regular Web Application with authorization-code grant
  enabled, register the exact Allowed Callback URL, and use the application
  client ID/secret. Enter the standard tenant hostname, e.g.
  `your-tenant.eu.auth0.com`, without `https://` or a path. The issuer is
  `https://{tenant}/` including its trailing slash. Use client secret **POST**
  token endpoint authentication (`client_secret_post`). Auth0 custom domains
  are deliberately **not supported in this increment**: use the standard tenant
  domain consistently for issuer, authorization and callback testing.
* **Okta:** create an OIDC application using authorization-code grant + PKCE,
  assign the intended test account, register the exact sign-in redirect URI,
  and enter the org hostname (`*.okta.com`, `*.oktapreview.com` or
  `*.okta-emea.com`). Choose the authorization server ID: `default`, or its
  actual custom identifier. The issuer is
  `https://{org}/oauth2/{authorizationServerId}`. A default/custom authorization
  server requires the appropriate Okta entitlement; the org issuer without
  `/oauth2/{id}` is not included. For a confidential app configure
  `token_endpoint_auth_method=client_secret_post` (through your Okta app/API
  configuration). Alternatively use an authorization-code public PKCE app
  with `token_endpoint_auth_method=none` and no saved client secret, if permitted
  by your organization. Custom vanity domains are not supported.
* **Microsoft Entra ID:** create a single-tenant app registration and Web
  redirect URI, record the Application (client) ID, create a client secret and
  supply its **value**, not its credential ID. Enter the Directory (tenant) ID
  as a nonzero GUID. Authority is
  `https://login.microsoftonline.com/{tenantGuid}/v2.0`. Shared `common` /
  `organizations` endpoints, tenant names and sovereign-cloud hosts are
  intentionally rejected. Grant any organizational consent required for
  `openid profile email`; no Graph API or userinfo permissions are needed.

Tests request only the `openid` scope. The test identity needs permission to
authenticate to the provider application; it does **not** need to match the
local administrator email. External claims are discarded after validation.
Effective login requests `openid profile email`. ID-token claims supply the
profile; there is no userinfo/Graph request. The configured client uses
`client_secret_post` when a secret is saved, or PKCE without a secret for a
provider-supported public client. The login handler shares the maintained
framework protocol options/validator and SSRF-protected backchannel with tests.
Both paths explicitly use the standard authorization-code + PKCE redirect,
not automatically advertised optional Pushed Authorization Requests (PAR).
Some provider applications reject PAR even when discovery advertises its endpoint;
the framework's default automatic PAR behavior can otherwise fail before browser
redirection. Providers declaring PAR mandatory are rejected, never silently
downgraded. Supporting mandatory PAR requires a separately configured integration.

## Draft, test and activation workflow

1. Sign in as the local platform administrator (or an explicitly approved linked
   SSO administrator) and open the authentication page.
2. Add a provider draft, supply its tenant/client ID and choose a secret action:
   **retain** leaves an existing secret unchanged; **replace** requires a new
   value; **clear** removes it for a public PKCE application. No masked value
   is preloaded/submitted. API responses contain only `hasSecret`.
3. Save. Every edit increments the revision and invalidates its test result,
   but does not change the previous active configuration.
4. Select **Test connection**. The protected POST creates a ten-minute attempt
   bound to the initiating local or approved SSO session and exact draft revision. Discovery
   is followed by a real browser authorization request. Authenticate at the
   provider; the API exchanges the returned code using PKCE and validates
   signature, issuer, audience, lifetime, nonce and correlation with the
   maintained ASP.NET Core OpenID Connect handler. It never signs an external
   ticket in or creates a user/session.
5. On return, the page refreshes authoritative database status. A URL query
   outcome is feedback only and cannot mark a configuration tested. Select
   **Activate verified configuration** separately. Activation atomically
   snapshots that revision, including the encrypted credential. Failed tests
   or subsequent draft edits do not overwrite that active snapshot.
6. `/login` shows the active provider buttons. **Deactivate** removes the active
   copy and revokes its SSO sessions/attempts; the draft remains. An inactive
   provider can be deleted. Local administrator access remains independent.

## User login, account linkage and ownership

The anonymous `GET /auth/sso/login/providers` returns only active snapshot
`id`, `name`, `type`. No tenant/client identifiers, secrets, drafts or roles are
disclosed. `POST /auth/sso/login/{id}/start` requires the same exact trusted
Origin and `X-Session-Protection: 1` as local authentication, and shares its
per-source-IP 10 attempts/minute limit. It returns a framework-generated
authorization URL; there are no arbitrary return URLs. Successful callbacks
redirect only to the configured frontend `/dashboard`; failures go to
`/login?ssoError=login_failed` or `email_collision`, never raw IdP messages.

The separate login handler uses authorization code, PKCE S256, encrypted/authenticated
framework state and same-browser nonce/correlation. A random, ten-minute DB
attempt stores the active revision/snapshot and an optional prior session hash,
**not** tokens, plaintext PKCE or credentials. It is atomically consumed before
code exchange. The callback rechecks the active snapshot in the JIT/session
transaction under the same PostgreSQL advisory lock as provider mutations.
Invalid signature, issuer, audience, lifetime, nonce, correlation, state, replay,
expiry, disabled provider or changed active snapshot all fail closed.

`ExternalIdentities` uniquely keys the exact trusted **issuer + subject** pair
to `users.Id`; never email, name, groups, role claims or first-login order.
Concurrent first callbacks are serialized and constrained by the unique DB
index; repeat logins retain the same local ID. Two active configurations using
the same issuer reuse a pair only after independently validating the selected
provider's audience and current snapshot. A different issuer or subject creates
a distinct identity. The IdP's `PlatformAdministrator` claim is ignored. JIT accounts remain `User`.
Local-administrator identities are rejected unless that exact external identity
has persisted `AdministratorAccessApproved = true` from the explicit flow below.

Email is optional: absent or malformed email yields `NULL`, never a fabricated
address. Valid email may be saved even when unverified; `EmailVerified` is true
only for an explicit `email_verified=true` claim, false otherwise. Email is
profile information, never authorization or account recovery evidence. Name
is a bounded non-control-character claim or the neutral fallback `SSO user`.
An email collision with **any existing user**, including the local administrator,
fails explicitly with `email_collision` and creates no identity/account/session.
There is no automatic email matching/linking/promotion. An existing local
administrator can instead use explicit linking below; no Okta email change is
needed. Ordinary JIT accounts cannot use a cached SSO session as password proof.
Existing linked users are not overwritten by later email/name claims.

New users have no inherited memberships or seeded/other-owner data. Existing
owner checks govern every team API; ordinary users can create and own their
own teams normally. No organization or import behavior is added.

## Revocable sessions and recovery

### Explicit local-administrator linking and revocation

1. Apply `AddExplicitAdministratorAccountLinking` with the migration command above.
2. Sign in at `/admin/login` with the **existing local recovery credentials**,
   even if already signed in through SSO. Open **Account security**
   (`/account/security`) in the administrator menu.
3. Select a **currently active** provider, not a saved draft. Enter your local
   password again and explicitly consent to retaining the existing administrator
   role. Each start requires a new password confirmation; a cached profile or
   SSO session is never sufficient.
4. Authenticate freshly at the provider. Linking requests `prompt=login` and
   `max_age=0`, with the same framework code/PKCE validator, nonce/correlation,
   trusted issuer and SSRF-protected backchannel as effective login.
   The validated ID token must include `auth_time` at or after link initiation
   (with 30-second clock tolerance); missing/stale/future authentication times
   are rejected, not silently treated as fresh provider proof.
5. The fixed configured frontend `/account/security?link=success` shows feedback;
   the freshly loaded linked-identity list is authoritative. No raw subject,
   claims, tokens or password is returned. **Linking leaves the original local
   session and cookie intact**. Log out and use `/login` when you want to test SSO.

`POST /auth/account/identities/start` accepts only a provider ID, local password
and explicit administrator-access consent. The target user, role and original
local session hash are server-selected. Password verification, Identity rehash
and serialized failed-attempt/15-minute lockout behavior are shared with local
login. Wrong passwords never rotate/revoke a valid session or write identities.
The shared authentication rate limiter and exact Origin/header CSRF rules apply.

The ten-minute single-use attempt stores the target existing user ID and exact
source local session hash. The callback uses DB proof, **not** the Strict cookie
which is absent on cross-site returns. It rechecks/locks the active credential,
user and original local session and verifies the exact active snapshot under
the same PostgreSQL advisory lock as provider mutations. Logout, session
rotation/expiry, credential/user deactivation, changed role, provider reactivation
or deletion of the attempt invalidates the proof, including during token exchange.
Invalid/canceled/replayed/tampered protocol returns create no identity or session.
If the state/attempt is no longer available, feedback can fall back to the safe
login-failed page; return to Account security to restart.

The callback attaches the validated exact issuer/subject to the **existing
UserId**, preserving email, name, credential, team ownership and all existing
owner data. Email need not match: local password plus provider identity proof,
not email, authorizes the association. An identity owned by any different user
fails with `identity_conflict`; no account merge is allowed. Repeating the same
identity for the same user is idempotent, but requires both proofs again.
Only this deliberate flow writes `ExternalIdentities.AdministratorAccessApproved`.
Existing identities default to false on migration; merely having a local
credential, verified email, provider groups or a role claim grants nothing.

Subsequent SSO login resolves the existing UserId. Every SSO request computes
its role from the persisted identity approval and active local credential/user
with the existing `PlatformAdministrator` role. Revoking approval, removing or
deactivating the credential/user, or changing that role rejects existing admin
SSO sessions on their next request. `/auth/current` returns that actual role.
Approved SSO administrators can perform provider CRUD/test/activation and
connection callbacks also verify their exact live SSO source session and approval.
Ordinary accounts remain ordinary and cannot access these administration endpoints.

To revoke a link, sign in **locally**, enter the local password on Account
security, then choose **Unlink with password confirmation**. The protected
`POST /auth/account/identities/{identityId}/unlink` atomically removes only your
identity, revokes its SSO sessions and invalidates your pending link attempts.
It never deletes the local account, password or teams. A later SSO login with
the same local-account email again fails `email_collision` until explicitly
linked again. The local recovery form remains accessible to signed-in SSO
administrators. Audit logging includes only safe local IDs and fixed outcomes;
never enable provider bodies, subject/email or credential logging.

`SsoSessions` contains only a hash of an opaque random eight-hour token and
references the user, external identity, provider and active snapshot/revision.
Every SSO API request rechecks
account activity, identity/account consistency and the exact current active
provider snapshot. Draft edits leave sessions and in-flight logins unaffected.
Activation (including reactivation) conservatively deletes that provider's
existing sessions and consumes outstanding attempts; deactivation does likewise.
Active revision/configuration mismatches also immediately reject a session.
Deleting accounts/providers cascades the applicable records.

For compatibility, local and SSO sessions share the one `aem.admin.session`
cookie slot: successful switching rotates it and revokes the presented prior
session, so no competing local/SSO cookies can select a surprising privilege.
Local cookies/scheme retain their existing behavior. Both modes protect
mutations against CSRF and `/auth/current` and `/auth/logout` accept only these
session schemes, not bearer-only impersonation. Bearer remains preferred when
explicitly supplied on other API requests. Provider administration uses `PlatformAdministrator`: a local or approved SSO
session with a server-authoritative existing administrator role. Bearer role
claims alone are never accepted. `LocalAdministrator` remains local-only for
password reauthentication, linking and unlinking.

Logout revokes the app session and clears the cookie/team context; it does not
log out of Okta/Auth0/Entra. A later login can transparently reuse the provider's
upstream session. No tokens are stored in frontend localStorage. The independent
local recovery form remains `/admin/login`; provider outage/deactivation cannot
lock the administrator out. Revocation at the upstream provider alone does not
revoke a preexisting app session in this pilot: deactivate the provider, disable
the app account or delete sessions operationally when immediate revocation is
needed. Backchannel logout and organization lifecycle management are deferred.

Only the newest connection-test attempt for a provider can complete. Attempts are single-use;
replay, timeout, revision changes, session logout/rotation/expiry, inactive
administrator, missing correlation/nonce or invalid tokens fail closed.
The main local session cookie retains `SameSite=Strict`; the callback uses
short-lived framework `HttpOnly`, `SameSite=Lax` nonce/correlation cookies for
the GET cross-site return and checks that the initiating DB session still
exists and is valid. Restart/multiple replicas require shared Data Protection
keys to decode state and credentials. If a test fails, check exact callback,
app grant/authentication method, secret expiry, assigned test user and tenant.
Save any corrections, retry and then activate. Retesting clears the draft's
verified status until a fresh success, not the old active snapshot.

## Secrets, key rings and outbound restrictions

Credentials use the existing `ISecretProtector` /
`DataProtectionSecretProtector`; active snapshots contain the same ciphertext,
not plaintext. They are never returned. Data Protection also authenticates and
encrypts state containing the PKCE verifier and attempt ID. Tokens and PKCE are
never persisted; effective login stores only issuer/subject and the limited
profile fields described above.

Existing `AddDataProtection()` defaults persist keys to the host user's profile
when available (typically `~/.aspnet/DataProtection-Keys` on macOS/Linux), but
ephemeral containers or hosts without a usable profile need explicit durable
storage. Set optional `Authentication__DataProtection__KeyRingPath` to a
persistent, access-restricted directory mounted identically on all API replicas.
This setting applies to **all** existing Data Protection secrets, including
integration credentials. When moving key storage, copy the existing key ring;
keep the same application/content-root discriminator. Do not discard old keys.
Back up the database and key ring together, protect keys at rest with your
platform's encrypted storage/access controls, and do not expose or commit
keys. Filesystem persistence alone does not encrypt key files on macOS/Linux.
Configure a platform-specific managed key-protection provider before deployment
if your threat model requires it. Lost keys require credential replacement and
a new test; there is no plaintext recovery path.

Authorities are derived from typed standard tenant inputs, not arbitrary URLs.
ASCII hostnames only, no IDN/punycode, ports, paths, credentials or lookalike
suffixes. Outbound discovery, JWKS and token URLs must be HTTPS on the **exact
configured provider hostname** on port 443. Redirects and proxy use are disabled.
DNS is resolved at socket connection time; any private, loopback, link-local,
reserved or transition address causes rejection, and connections use the
validated addresses directly to prevent DNS rebinding. No userinfo fetch occurs.
Providers with cross-host metadata/JWKS URLs or custom domains require a future
explicit trust design rather than weakening these defaults.

## Troubleshooting a failed login start

A `400` with `error: login_failed` from `/auth/sso/login/{providerId}/start`
now includes an opaque `diagnosticId`. The login page displays this reference.
Find the matching `SSO login preparation failed` warning in the API console or
your restricted server logs. It reports only the provider ID, active revision,
preparation stage, exception type names, an optional HTTP status, a fixed-format
IdentityModel error code and an allowlisted OAuth error hint. Exception
messages, stack traces, credentials, callback codes, tokens and upstream bodies
are deliberately not logged or returned. Do not enable PII or HTTP body logging.

`SecretDecryption` points to key-ring/credential protection; `Options` or
`HandlerInitialization` points to local protocol setup; `AuthorizationChallenge`
includes provider discovery, metadata retrieval and authorization-request
preparation. An HTTP status can identify a rejected metadata request. These are
diagnostic leads, not proof of a specific root cause. Supply only the filtered
warning and reference when requesting support. No database migration is required
for this diagnostic increment; restart the API and frontend to use it.

## Focused validation

Frontend session state, HTTP session operations, the interceptor and guards remain
in `frontend/src/app/core/auth`. Login/setup/local-administrator pages and
`PublicSsoApi` live in `features/authentication`; provider settings and `SsoApi`
live in `features/administration`; account security and `AccountLinkApi` live in
`features/account`. The account feature explicitly imports the shared public
authentication API rather than duplicating it. Co-located tests follow these
owners. Translation resources live under `public/i18n/authentication` for
`sso.login.*`, `public/i18n/account` for `accountLink.*`, and
`public/i18n/administration` for the remaining `sso.*` provider-setting keys.
The historical keys and text remain unchanged: the startup multi-resource
loader deep-merges the disjoint `sso` subsets, rather than using a catch-all
resource. Reusable language/team selectors and table pagination live in
`shared/ui`; global i18n and team-selection state remain in `core`.

With the repository PostgreSQL test service on port 5433:

```sh
dotnet test tests/AiEngineeringManagerCopilot.IntegrationTests \
  --filter 'FullyQualifiedName~Sso|FullyQualifiedName~LocalAuthenticationEndpointsTests|FullyQualifiedName~AccountLinkEndpointsTests'
dotnet test tests/AiEngineeringManagerCopilot.UnitTests \
  --filter 'FullyQualifiedName~Authentication|FullyQualifiedName~ServiceRegistrationTests'
dotnet ef migrations has-pending-model-changes \
  --project src/AiEngineeringManagerCopilot.Infrastructure \
  --startup-project src/AiEngineeringManagerCopilot.Api
cd frontend
npm test -- --watch=false --include='src/app/features/authentication/**/*.spec.ts' \
  --include='src/app/features/administration/**/*.spec.ts' \
  --include='src/app/features/account/**/*.spec.ts' \
  --include='src/app/core/auth/*.spec.ts' \
  --include='src/app/core/i18n/**/*.spec.ts' \
  --include='src/app/shared/**/*.spec.ts' \
  --include='src/app/app.routes.spec.ts' \
  --include='src/app/core/app/app-initializer.spec.ts'
npm run build -- --configuration production
```

The protocol tests use isolated PostgreSQL schemas, real local authentication,
the actual framework OIDC handler, fake discovery/JWKS/token HTTP responses and
RSA-signed ID tokens. They verify code exchange/PKCE and invalid signature,
issuer, audience, expiry, nonce, state, correlation, revoked session, changed
revision and replay handling.
The effective-login tests also cover JIT concurrency, stable issuer/subject
linkage across providers, optional/unverified email, administrator email
collision, owner isolation, logout/account switching, active vs draft changes,
in-flight changes, revocation, protected/rate-limited starts and prefixed callbacks.
Explicit-link tests cover local-password confirmation/lockout, consent/CSRF,
source revocation during exchange, invalid tokens/state/correlation/replay,
identity conflict/idempotent concurrency, retained owner data and local session,
approved SSO provider CRUD/connection testing, dynamic approval revocation and
password-confirmed unlink.
They are **not live tenant validation**. Operators
must provide real provider applications/credentials and run the browser test
for each intended tenant before activation.
