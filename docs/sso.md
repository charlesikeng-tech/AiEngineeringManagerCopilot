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
external administrator linking. Provider configuration is global to this
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

1. Sign in as the local platform administrator and open the authentication page.
2. Add a provider draft, supply its tenant/client ID and choose a secret action:
   **retain** leaves an existing secret unchanged; **replace** requires a new
   value; **clear** removes it for a public PKCE application. No masked value
   is preloaded/submitted. API responses contain only `hasSecret`.
3. Save. Every edit increments the revision and invalidates its test result,
   but does not change the previous active configuration.
4. Select **Test connection**. The protected POST creates a ten-minute attempt
   bound to the initiating local session and exact draft revision. Discovery
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
a distinct identity. The IdP's `PlatformAdministrator` claim is ignored: every
SSO principal is `User`, and linked local-administrator records are rejected.

Email is optional: absent or malformed email yields `NULL`, never a fabricated
address. Valid email may be saved even when unverified; `EmailVerified` is true
only for an explicit `email_verified=true` claim, false otherwise. Email is
profile information, never authorization or account recovery evidence. Name
is a bounded non-control-character claim or the neutral fallback `SSO user`.
An email collision with **any existing user**, including the local administrator,
fails explicitly with `email_collision` and creates no identity/account/session.
There is no email matching/linking/promotion. The provider administrator must
resolve the claim or omit it; there is no manual linking UI in this pilot.
Existing linked users are not overwritten by later email/name claims.

New users have no inherited memberships or seeded/other-owner data. Existing
owner checks govern every team API; ordinary users can create and own their
own teams normally. No organization or import behavior is added.

## Revocable sessions and recovery

`SsoSessions` contains only a hash of an opaque random eight-hour token and
references the user, external identity, provider and active snapshot/revision.
It does not reference `LocalAdministrators`. Every SSO API request rechecks
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
explicitly supplied on other API requests. Provider administration still
requires local scheme + explicit local `PlatformAdministrator` role.

Logout revokes the app session and clears the cookie/team context; it does not
log out of Okta/Auth0/Entra. A later login can transparently reuse the provider's
upstream session. No tokens are stored in frontend localStorage. The independent
local recovery form remains `/admin/login`; provider outage/deactivation cannot
lock the administrator out. Revocation at the upstream provider alone does not
revoke a preexisting app session in this pilot: deactivate the provider, disable
the app account or delete sessions operationally when immediate revocation is
needed. Backchannel logout and organization lifecycle management are deferred.

Only the newest attempt for a provider can complete. Attempts are single-use;
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

With the repository PostgreSQL test service on port 5433:

```sh
dotnet test tests/AiEngineeringManagerCopilot.IntegrationTests \
  --filter 'FullyQualifiedName~Sso|FullyQualifiedName~LocalAuthenticationEndpointsTests'
cd frontend
npm test -- --watch=false --include='src/app/features/authentication/*.spec.ts' \
  --include='src/app/core/auth/*.spec.ts' \
  --include='src/app/core/i18n/i18n.providers.spec.ts' \
  --include='src/app/core/layout/main-layout/main-layout.spec.ts' \
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
They are **not live tenant validation**. Operators
must provide real provider applications/credentials and run the browser test
for each intended tenant before activation.
