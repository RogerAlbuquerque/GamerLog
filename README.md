# GameLog

A social gaming journal in the spirit of Letterboxd: log the games you play, rate them to the half star,
write spoiler-safe reviews, keep playthrough diaries, build collections, follow friends, and look back on
your year in games.

| | |
|---|---|
| **Web** | Next.js 15 (App Router), React 19, TypeScript, Tailwind CSS 4, Zustand, React Hook Form + Zod |
| **API** | ASP.NET Core Web API on **.NET 10**, C#, Entity Framework Core, PostgreSQL |
| **Infra** | Docker Compose, S3-compatible object storage (MinIO), Swagger/OpenAPI |
| **Catalog** | Local-first catalog, enriched from **IGDB** by an administrator |

> **Read this first:** the repository was authored in an environment without access to NuGet, so the .NET
> backend was type-checked and unit-tested offline against stand-in libraries but **never compiled against the
> real packages**. Details and the exact list of what was and was not executed are in
> [docs/validation.md](docs/validation.md). The frontend was fully built, linted and tested.
> The Git history is synthetic, see [docs/git-history.md](docs/git-history.md).

## Features

- **Accounts:** register, sign in, profile, avatar upload, spoiler preference, password change, account deletion.
- **Catalog:** search with filters and sorting, game pages, related games, IGDB import (admin).
- **Diary:** status per game (playing, completed, backlog, …), multiple playthroughs with platform, dates and hours.
- **Ratings & reviews:** half-star ratings with a histogram, structured reviews (pros/cons, difficulty, favourites), spoiler flags.
- **Social:** threaded comments, likes, follows, activity feed, notifications.
- **Collections:** public or private, ordered, with covers.
- **Insights:** statistics, discovery rails, explainable rule-based recommendations, yearly retrospective with a shareable card.
- **Security:** hashed passwords, cookie sessions, CSRF protection, CORS allow-list, rate limiting, validated image uploads, XSS-safe rendering ([docs/security.md](docs/security.md)).

## Quick start with Docker (recommended)

Requirements: Docker Engine with the Compose plugin.

```bash
cp .env.example .env
# edit .env: set POSTGRES_PASSWORD, S3_ACCESS_KEY, S3_SECRET_KEY to long random values
docker compose up --build
```

Open <http://localhost:3000>. The API applies migrations and loads a starter catalog on first start.
To import more games from IGDB, see [IGDB](#igdb-optional).

| Service | URL |
|---|---|
| Web | <http://localhost:3000> |
| API (loopback only) | <http://localhost:5080> (Swagger: set `SWAGGER_ENABLED=true`) |
| MinIO console | <http://localhost:9001> |
| PostgreSQL | `127.0.0.1:5432` |

## Local development without Docker

### Prerequisites on Fedora

```bash
# .NET 10 SDK (use the Microsoft feed or the distribution package if available)
sudo dnf install -y dotnet-sdk-10.0
# Node.js 22 and Git
sudo dnf install -y nodejs npm git
# PostgreSQL 16
sudo dnf install -y postgresql-server postgresql-contrib
sudo postgresql-setup --initdb
sudo systemctl enable --now postgresql
```

Verify with `dotnet --version` (10.x) and `node --version` (22.x). If `dotnet-sdk-10.0` is not yet in your
Fedora release, install the SDK from <https://dotnet.microsoft.com/download/dotnet/10.0>.

### Configure PostgreSQL

```bash
sudo -u postgres psql <<'SQL'
CREATE USER gamelog WITH PASSWORD 'change-me-local-only';
CREATE DATABASE gamelog OWNER gamelog;
\c gamelog
CREATE EXTENSION IF NOT EXISTS pg_trgm;
SQL
```

Fedora's default `pg_hba.conf` uses `ident` for TCP connections. Change the `127.0.0.1/32` and `::1/128` lines
to `scram-sha-256`, then `sudo systemctl restart postgresql`.

The development connection string lives in `api/src/GameLog.Api/appsettings.Development.json`
(`Host=localhost;Database=gamelog;Username=gamelog;Password=change-me-local-only`). Override it without editing
files: `export ConnectionStrings__Default="Host=localhost;Database=gamelog;Username=gamelog;Password=..."`.

### Run the API

```bash
cd api
dotnet run --project src/GameLog.Api      # http://localhost:5080, Swagger at /swagger
```

Development mode applies migrations, seeds the starter catalog and stores uploads in a local folder
(`Storage:Provider=filesystem`), so MinIO is not required.

### Run the web app

```bash
cd web
npm ci
npm run dev                               # http://localhost:3000, proxies /api/* to the API
```

## IGDB (optional)

1. Create a Twitch developer application at <https://dev.twitch.tv/console> and copy its client id and secret.
2. Set `IGDB_CLIENT_ID` and `IGDB_CLIENT_SECRET` (in `.env` for Docker, or `Igdb__ClientId` / `Igdb__ClientSecret`).
3. Create the first administrator by setting `SEED_ADMIN_USERNAME`, `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD`
   (Docker) or `Seed__AdminUsername` etc. (local) for one start, then unset the password.
4. Sign in as that user and open **Import games** in the account menu.

No request from a regular user ever depends on IGDB, so the app keeps working if the provider is unavailable.

## Tests and checks

```bash
cd api && dotnet test GameLog.sln          # backend unit + integration tests
cd web && npm run lint && npm run typecheck && npm test && npm run build
```

See [docs/testing.md](docs/testing.md).

## Repository layout

```
api/            ASP.NET Core solution (Domain, Application, Infrastructure, Api, tests)
web/            Next.js application
docs/           architecture, database, API reference, security, migrations, validation, ADRs
scripts/        maintenance scripts
docker-compose.yml, .env.example
```

## Documentation

- [Architecture](docs/architecture.md) · [Database](docs/database.md) · [API contract](docs/api-contract.md) · [API reference](docs/api-reference.md)
- [Security](docs/security.md) · [Testing](docs/testing.md) · [Migrations](docs/migrations.md)
- [Validation report and limitations](docs/validation.md) · [Git history notice](docs/git-history.md)
- [Architecture decision records](docs/adr)

## Troubleshooting

- **Cannot sign in over plain HTTP on a non-localhost host:** the session cookie is `Secure`; use HTTPS or `localhost`.
- **`POSTGRES_PASSWORD` error from Compose:** copy `.env.example` to `.env` and fill the required values.
- **Port already in use:** change `WEB_PORT` / `API_PORT` in `.env`.
- **Covers are blank:** the starter catalog has no artwork; import from IGDB to fetch covers.
