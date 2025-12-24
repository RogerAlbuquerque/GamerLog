# Validation report and known limitations

This report separates what was **executed** from what was **only written**. Nothing here is claimed
beyond what was actually run in the authoring environment, which had no access to NuGet.org
(Node/npm packages and a local PostgreSQL 16 were available).

## Frontend — fully verified

| Check | Command | Result |
|---|---|---|
| Type check | `npm run typecheck` | passes |
| Lint | `npm run lint` | passes |
| Unit/component tests | `npm test` | 31 files, 160 tests, all pass |
| Production build | `npm run build` | succeeds; every route compiles |

## Backend — written carefully, **not compiled against the real packages**

The .NET 10 packages (ASP.NET Core shared framework, EF Core, Npgsql, ImageSharp, AWS S3 SDK, Swashbuckle,
xUnit, …) could not be restored. To reduce risk, the sources were checked with a private offline harness (not
part of this repository) that compiles every project against hand-written stand-ins for those libraries and runs
the xUnit tests with a small reflection-based runner.

| Check | Result |
|---|---|
| Type-check of Domain, Application, Infrastructure and Api against stand-ins | succeeds |
| Test projects compile | succeeds (unit and integration) |
| Unit tests that run on pure logic or fakes | **202 passed, 0 failed** |
| Unit tests that need a real EF Core provider (SQLite in-memory) | **137 compiled but not executed** |
| Integration tests (`WebApplicationFactory`) | compiled only; **not executed** |

A stand-in is only as accurate as its author, so a clean offline build does **not** prove the code compiles
against the real libraries. Expect to fix a small number of compile errors or API mismatches on the first
real `dotnet build`.

### Database

- The migration DDL was translated to SQL and applied to PostgreSQL 16: all 18 tables, constraints and indexes
  were created without errors, and a script compared entity properties to columns (one known false positive,
  `Games.GameModes`, see [migrations.md](migrations.md)).
- No model snapshot exists because `dotnet ef` could not run; use `scripts/regenerate-migrations.sh`
  ([ADR 0006](adr/0006-hand-written-migrations.md)).

## Not executed anywhere

- `docker compose up --build` (no Docker daemon): the Dockerfiles and `docker-compose.yml` were written and the
  YAML parsed, but images were never built.
- Real S3/MinIO uploads, ImageSharp re-encoding, IGDB calls, Swagger generation, the HTTP resilience handler,
  rate-limiter behaviour under load, and a browser-based end-to-end session.
- The CI workflow has never run.

## Suggested first steps on a connected machine

```bash
cd api && dotnet restore && dotnet build && dotnet test        # fix any compile errors first
./scripts/regenerate-migrations.sh                              # canonical migrations + snapshot
cp .env.example .env && docker compose up --build               # end-to-end smoke test (docs/testing.md)
python3 scripts/validate_history.py . report.md                 # Git history checks
```

## Git history

The history is synthetic (an educational Git lab); see [git-history.md](git-history.md). It is verified by
`scripts/validate_history.py` and by an independent one-liner for `AuthorDate == CommitterDate`.

## Other known limitations

- No email verification or password reset.
- Rate-limit counters are per API instance (in memory).
- The activity feed `total` counts stored items and can exceed the number of visible ones after hidden items are
  filtered out.
- Starter catalog (30 games) has no artwork; covers come from IGDB imports.
