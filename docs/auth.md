# Authentication

## Stack

Use:

- ASP.NET Core Identity
- JWT Access Tokens
- Refresh Tokens
- Roles
- Policies

## Roles

```text
SuperAdmin
CompanyAdmin
SalesManager
SalesAgent
```

## Login Flow

1. User submits credentials.
2. Validate credentials using ASP.NET Core Identity.
3. Verify user is active.
4. Verify company is active for tenant users.
5. Generate short-lived JWT access token.
6. Generate cryptographically secure refresh token.
7. Store only refresh token hash.
8. Return authentication result using the selected secure frontend strategy.

## JWT Claims

Keep claims minimal. Recommended:

```text
sub = UserId
company_id = CompanyId
role = Roles
name = FullName
```

`role` is deliberately the short claim name, not .NET's `ClaimTypes.Role` long URI — the web client's JWT decode reads the token's raw payload keys directly (no server-side claim-type mapping applies there), so the long URI would leave `role` unreadable client-side. See `JwtTokenGenerator.cs` for why this matters and how `[Authorize(Roles=...)]` still works server-side. `name` (the user's FullName) was added for UI display only (e.g. the navbar) — never a security boundary, same as `role`/`company_id`. Do not put any other user/company data inside JWT beyond these four claims.

## Access Token

Access tokens should be short-lived. Do not store sensitive business data in access tokens.

## Refresh Tokens

Requirements:

- cryptographically secure
- hashed before database storage
- expiration
- revocation
- rotation
- replacement tracking

Never store plaintext refresh tokens. Changing a password (via `change-password` or a `forgot-password`/`reset-password` flow) revokes every other active refresh token for that user — a stolen token must not outlive a password change. See `AuthService.RevokeAllActiveRefreshTokensAsync`.

## Web cookie auth

The web SPA (only — see "Why not Flutter/the Public API" below) can opt into carrying its refresh token as a Secure, HttpOnly, SameSite cookie instead of localStorage. This is additive, not a replacement: the original JSON-body flow (`AuthResponse.RefreshToken` in the response, `RefreshRequest.RefreshToken` in the request) is completely unchanged and still what Flutter and every Public API/third-party integration use. A caller opts in by sending `X-Auth-Transport: cookie` on `login`; `refresh`/`logout` auto-detect it from the presence of the `rt` cookie itself. See `src/RealEstateCRM.Api/Auth/WebAuthCookies.cs` for the implementation.

### What changed on the web client

- **Access token**: kept in memory only (a module-level variable in

  `client/.../src/utils/authSession.ts`) — never localStorage. Lost on every page reload by design; `AuthProvider` silently re-fetches a fresh one on mount via the refresh-token cookie.

- **Refresh token**: never sent to the browser in a JSON body at all once cookie mode is active

  — `AuthController` blanks `AuthResponse.RefreshToken` before returning in that case. It only ever exists in the `rt` cookie: `HttpOnly` (unreadable by any JS, XSS included), `Secure` in non-Development, `SameSite=None` in non-Development (the API and web app are separate origins — see `docs/deployment.md`) or `SameSite=Lax` in Development (plain HTTP local dev has no TLS; `Lax` still works cross-port since port isn't part of a cookie's "site"), scoped to `Path=/api/auth` only.

- **CSRF**: a non-HttpOnly `XSRF-TOKEN` cookie is set alongside `rt`. The SPA reads it via

  `document.cookie` and echoes it back as an `X-CSRF-Token` header on `refresh` — the server rejects a cookie-mode `refresh` call (403) unless the header matches the cookie (constant-time comparison). This is the standard double-submit pattern: a cross-site attacker's page can trigger a request that *carries* the victim's cookie automatically, but cannot *read* it (same-origin policy) to forge a matching header.

- CORS gained `AllowCredentials()` (only usable alongside the existing explicit origin

  allow-list — never with `AllowAnyOrigin`).

### Why not Flutter or the Public API

Cookies are a browser-specific mechanism solving a browser-specific problem (a JS execution context, i.e. the page itself, being untrustworthy under XSS). Neither of the other two clients has that problem the same way:

- **Flutter**: already uses `flutter_secure_storage` — the OS Keychain/Keystore, which is

  arguably *more* isolated than a browser cookie jar (no origin/CORS/SameSite model to get wrong, no third-party-cookie browser-policy churn to track). There is no "page JS" that could be XSS'd in a compiled native app the way there is in a SPA.

- **Public API (`/api/v1`) / third-party integrations**: explicitly server-to-server or

  scripted clients (see `docs/public-api.md`) — there is no browser, no cookie jar, and `X-Api-Key`/Bearer JWT is the correct, conventional credential shape for that use case. Forcing either onto cookies would add real complexity (cookie jars, CSRF headers, origin handling) for zero security benefit, since neither has the threat model cookies defend against.

### Residual risk and forward-looking note

`SameSite=None` cross-origin cookies work in all current major browsers today, but are the same general category of mechanism browsers' ongoing third-party-cookie-restriction efforts (Privacy Sandbox and similar) are trending against — a *legitimate* first-party-ish two-domain app like this one is not the target of that effort, but it is worth re-checking this remains unaffected on a periodic basis, and is the reason the `Domain` attribute was deliberately left unset (defaulting to the exact issuing host) rather than widened. If the web app and API are ever deployed under a shared parent domain (e.g. `app.example.com` + `api.example.com` instead of two unrelated `*.azurewebsites.net` hosts), the cookie's `Domain` could be scoped to the shared parent to make it a true first-party cookie from the browser's perspective — an infrastructure/DNS decision outside this repository's control, same category as the TLS- termination note in `docs/deployment.md`.

## Rate limiting

`login`/`refresh`/`logout`/`forgot-password`/`reset-password` are rate-limited (10 requests/ minute per IP, the `Auth` policy in `Program.cs`) — these are all `[AllowAnonymous]`, so IP is the only available partition key. This defends against credential stuffing from a single source and forgot-password email-bombing.

## Account lockout

Added 2026-08-28, complementing the per-IP rate limiting above: IP limits do nothing against a distributed attempt spread across many addresses, each staying under the limit while all targeting one account.

Configured in `Infrastructure/DependencyInjection.cs`: 5 failed attempts, 15-minute lockout, `AllowedForNewUsers = true`. That last flag is load-bearing — `LockoutEnabled` is never set explicitly anywhere in this codebase, so it is taken from this option at `CreateAsync` time, and `IsLockedOutAsync` returns `false` whenever it is off.

`AuthService.LoginAsync` drives the bookkeeping itself rather than switching to `SignInManager` (which would pull in cookie authentication this JWT API does not use):

- a locked-out account is rejected **before** the password is checked, so a lockout cannot be waited out by continuing to guess;
- `AccessFailedAsync` on a wrong password — `CheckPasswordAsync` only verifies the hash and does none of this, which is why the `AccessFailedCount`/`LockoutEnd` columns had existed since the first Identity migration with nothing ever writing to them;
- `ResetAccessFailedCountAsync` on success, so failures accumulate toward the threshold but a genuine sign-in clears them.

**A lockout returns the same message and status as a wrong password.** A distinct "account locked" response would help the real user, but it also confirms the account exists — turning five failed attempts into an account-enumeration oracle, the exact property the generic unknown-email response exists to protect. Pinned by `LoginAsync_LockoutMessage_IsIndistinguishableFromWrongPassword`.

Covered by four tests in `AuthServiceTests`, verified to fail when the bookkeeping is removed rather than passing alongside it.

## Initial Endpoints

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
POST /api/auth/change-password
POST /api/auth/forgot-password
POST /api/auth/reset-password
```

Email confirmation can be enabled when email infrastructure exists.

## Authorization

Use both:

- Roles
- Policies

## CompanyAdmin

Can:

- manage company users
- view company reports
- manage projects
- manage units
- access company settings
- view company sales data

## SalesManager

Can:

- access team leads
- assign leads
- transfer leads
- see team performance
- manage team follow-ups

## SalesAgent

Can:

- access authorized/assigned leads
- add lead activities
- manage own follow-ups
- access allowed inventory
- manage authorized deals

## SuperAdmin

Platform-level administrator. SuperAdmin cross-tenant access must always be explicit. Do not globally bypass tenant protection just because the user is SuperAdmin.

## Frontend

React should support:

- login
- logout
- protected routes
- role-aware routes/navigation
- access token refresh
- session expiration

Frontend permissions are only UX. Backend authorization is mandatory.

## Security

Never:

- return password hashes
- log passwords
- log access tokens
- trust frontend role values
- trust frontend CompanyId
- expose unnecessary account-existence information
