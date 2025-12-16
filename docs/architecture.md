# Architecture

```
Browser ──► Next.js (App Router: server components + client islands)
              │  same-origin rewrite  /api/* ─► API
              ▼
        ASP.NET Core Web API ──► PostgreSQL
              ├──► S3-compatible storage (MinIO in development)
              └──► IGDB (import only, never on a read path)
```

## Backend layers (`api/src`)

| Project | Responsibility |
|---------|----------------|
| `GameLog.Domain` | Entities, enums and pure domain rules (diary transitions, rating bounds). No dependencies. |
| `GameLog.Application` | Use-case services holding **all business logic**, DTO contracts, abstractions (`IAppDbContext`, `IObjectStorage`, `IGameMetadataProvider`). |
| `GameLog.Infrastructure` | EF Core `AppDbContext`, entity configurations, migrations, S3 storage, image processing, IGDB client, seed data. |
| `GameLog.Api` | Thin controllers, authentication, CSRF, rate limiting, error handling, Swagger. |

Rules of thumb:

- Controllers map HTTP to a service call and nothing else.
- Services receive the acting `userId` explicitly; ownership is checked in the service, never trusted from the request body.
- Services use `IAppDbContext.Set<T>()` directly. EF Core is already a repository/unit of work, so no extra repository layer.

## Frontend (`web/src`)

- Route files in `app/` are thin: they read route params, set metadata and render a *view* component.
- Views are client components that fetch through `lib/api` (the only module that talks HTTP). The browser
  calls the Next.js origin; `next.config.ts` rewrites `/api/*` to the API, so the session cookie is first-party
  and the API port never needs to be public.
- Types in `lib/api/types.ts` mirror the API contracts; `lib/api/client.ts` adds the CSRF header, retries once
  when the token is stale, and maps RFC 7807 problems to `ApiError`.
- Global state is limited to two Zustand stores (`session`, `toast`); everything else is local state or
  `useAsync` results. Forms use React Hook Form with Zod schemas in `lib/schemas.ts`.
- User-written text is rendered through `Prose` as escaped text; spoilers are kept out of the DOM until revealed.
- Design tokens (ink/accent/mint/amber palette, `.surface`, `.btn`) are defined once in `app/globals.css`.

## Notable design decisions

- **Counters** (rating averages, like and comment counts) are denormalised and recomputed with a single SQL
  `UPDATE` after each change, which stays correct under concurrent writes (ADR 0002).
- **Activity and notifications** are staged on the EF change tracker by the service that causes them and saved
  atomically with the change, so a feed item can never exist for an action that failed.
- **Activity feed** merges events of the same kind by the same user on the same game within 24 hours.
- **Collections** are reordered in two phases (shift positions out of range, then write final positions) so the
  unique `(CollectionId, Position)` index is never violated mid-update.
- **Recommendations** are transparent rule-based scoring (no ML): genres and platforms the user completes or rates
  highly pull matching games up, abandoned or low-rated ones push them down, well-rated games get a small quality
  bonus, and games already in the diary are excluded. Each result carries the reasons ("because you like RPG").
- **Statistics and yearly recap** are computed by pure calculator classes over a flat projection, which keeps them
  trivially unit-testable.
- **Deleted comments** become placeholders so reply threads stay intact.

## Cross-cutting

- Errors are RFC 7807 problem details with a stable `code` extension.
- Every list endpoint is paginated with a hard page-size cap (50).
- Structured JSON logging with a correlation id per request.
- Configuration comes from environment variables; no secret is committed.
