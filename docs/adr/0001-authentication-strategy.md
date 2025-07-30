# ADR 0001 — Authentication strategy

**Status:** accepted

## Context

The web app and API are served from one origin (Next.js rewrites `/api/*`).
Tokens readable by JavaScript would be exposed to XSS.

## Decision

- ASP.NET Core **cookie authentication**: an encrypted ticket (Data Protection) in an
  `HttpOnly; Secure; SameSite=Lax` cookie with sliding expiration.
- Passwords hashed with the framework `PasswordHasher<TUser>` (PBKDF2, HMAC-SHA512, per-user salt, versioned format).
- Every request re-validates the user's **security stamp** so password changes and
  account deletion revoke existing sessions immediately.
- **CSRF:** built-in antiforgery. The SPA fetches a request token from
  `GET /api/v1/auth/csrf` and sends it as `X-CSRF-TOKEN` on every unsafe method.
- Roles are read from the database when the cookie is issued and re-checked by the
  security-stamp validator; clients can never supply roles or ownership.

## Alternatives considered

JWT in a cookie plus rotating refresh tokens: more moving parts (token storage,
rotation, revocation lists) and extra NuGet dependencies without a benefit for a
same-origin first-party app.

## Consequences

Sessions are server-validated and revocable. Scaling out needs a shared Data
Protection key ring (a mounted volume in Docker Compose).
