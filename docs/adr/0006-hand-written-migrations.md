# ADR 0006 — Hand-written migrations without a model snapshot

**Status:** accepted (temporary)

## Context

The project was authored in an environment that could not restore NuGet packages, so `dotnet ef migrations add`
could not run. The schema still had to be versioned, reviewable and applicable.

## Decision

Migrations are written by hand (`Infrastructure/Persistence/Migrations`) using `MigrationBuilder`, and were
checked by translating them to SQL and applying them to PostgreSQL 16. There is **no
`AppDbContextModelSnapshot`**, so `dotnet ef migrations add` cannot diff against them. `scripts/regenerate-migrations.sh`
replaces them with a canonical, scaffolded history, including the `pg_trgm` index that the model cannot express.

## Consequences

The schema is correct for first-time installs. Run the script once on a machine with package access, review the
diff, remove the `PendingModelChangesWarning` suppression and commit. Until then, do not add migrations by hand
on top without also updating the model. See [migrations.md](../migrations.md).
