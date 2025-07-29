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

- Server components render read-heavy pages (game details, profiles, discovery).
- Client components handle interaction (rating, diary controls, comments, forms).
- `lib/api` is the only module that talks HTTP; types in `lib/types.ts` mirror the API contracts.
- Mutations send an `X-CSRF-TOKEN` header obtained from `GET /api/v1/auth/csrf`.

## Cross-cutting

- Errors are RFC 7807 problem details with a stable `code` extension.
- Every list endpoint is paginated with a hard page-size cap.
- Structured JSON logging with a correlation id per request.
- Configuration comes from environment variables; no secret is committed.
