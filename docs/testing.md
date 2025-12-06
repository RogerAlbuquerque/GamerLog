# Testing

## Backend (xUnit)

```bash
cd api
dotnet test GameLog.sln
```

| Project | What it covers | Needs |
|---|---|---|
| `GameLog.Tests.Unit` | Domain rules, pure calculators (statistics, yearly recap, recommender, rating math), services (ratings, reviews, comments, notifications, follows, activity feed, collections, discovery, diary, auth, catalog import) against an in-memory SQLite database. | .NET 10 SDK |
| `GameLog.Tests.Integration` | The real HTTP pipeline through `WebApplicationFactory` (cookie auth, antiforgery, rate limiting, RFC 7807 errors, social flows) with the DbContext swapped for SQLite. | .NET 10 SDK |

No PostgreSQL, MinIO or IGDB account is needed to run the automated tests; external services are replaced with fakes.

## Frontend (Vitest + Testing Library)

```bash
cd web
npm ci
npm run lint        # ESLint
npm run typecheck   # tsc --noEmit
npm test            # Vitest (jsdom)
npm run build       # production build, also type-checks every route
```

Covered: API client (CSRF retry, error mapping), Zustand stores, Zod schemas, form helpers, pure formatting/reorder utilities, UI primitives (spoiler, pagination, star rating, prose), the login/register forms, and review/comment components including markup-injection checks.

## Manual smoke test

1. `docker compose up --build`, open <http://localhost:3000>.
2. Register, open a game from **Discover**, rate it, log a playthrough, write a review.
3. Register a second user in a private window, follow the first, comment and like.
4. Check **Feed**, **Notifications**, **Stats** and **Retrospective** for the first user.
