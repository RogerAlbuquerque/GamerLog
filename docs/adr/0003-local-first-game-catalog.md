# ADR 0003 — Local-first game catalog

**Status:** accepted

## Decision

- All user-facing reads hit PostgreSQL only. IGDB is used exclusively by an
  admin-triggered **import**, never on a request path.
- `IGameMetadataProvider` abstracts the provider. `IgdbClient` implements it (Twitch
  OAuth client-credentials, Apicalypse queries, token cached in memory).
- `GameImportService` upserts by `(ExternalProvider, ExternalId)`, deduplicates genres
  and platforms by slug and copies artwork into our own object storage.
- Outbound calls use a resilience pipeline (retry, circuit breaker, timeout).
- A committed seed catalog lets the app run completely offline without IGDB keys.

## Consequences

If IGDB is down or unconfigured, only importing is affected. Catalog data may lag the
provider until the next import, which is acceptable for a journal application.
