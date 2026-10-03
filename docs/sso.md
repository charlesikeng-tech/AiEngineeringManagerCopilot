# Installation-wide SSO provider configuration and connection testing

This increment adds the platform administrator page `/admin/authentication`.
It saves Auth0, Okta and Microsoft Entra ID drafts, performs **real OIDC
authorization-code + PKCE** tests, and explicitly activates a successfully
tested revision. Activation currently means **a verified saved configuration,
not an enabled login method**. Normal login remains `/admin/login`. There are
no SSO login buttons, external administrator links, role assignment by email,
public signup, invitations, user import or ordinary-user provisioning. Provider
configuration is global to this installation; organization scoping is a later
increment. Deactivation never disables or deletes the local recovery account.

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

The exact redirect URI is:

* Development: `http://localhost:5249/auth/sso/callback`
* Example production: `https://copilot.example.com/api/auth/sso/callback`

The page displays the URL derived from the configured API base. Register it
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
  `openid`; no Graph API or userinfo permissions are needed.

Tests request only the `openid` scope. The test identity needs permission to
authenticate to the provider application; it does **not** need to match the
local administrator email. External claims are discarded after validation.

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
6. **Deactivate** removes the active copy; the draft remains. An inactive
   provider can be deleted. Local administrator login/logout is unchanged.

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
encrypts state containing the PKCE verifier and attempt ID. Neither token nor
sensitive external claims are persisted.

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
revision and replay handling. They are **not live tenant validation**. Operators
must provide real provider applications/credentials and run the browser test
for each intended tenant before activation.
