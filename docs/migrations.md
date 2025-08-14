# Database migrations

Migrations live in `api/src/GameLog.Infrastructure/Persistence/Migrations` and are applied
automatically at API start-up (`Database.MigrateAsync()`), controlled by `Database:AutoMigrate`.

## Why they are hand-authored

The initial repository was produced in an environment without NuGet access, so
`dotnet ef` could not run. The migrations were therefore written by hand from the
entity configurations and carry **no `AppDbContextModelSnapshot`**. Consequences:

- `Migrate()` works: it only needs the `[Migration]` classes. The pending-model-changes
  check is disabled in `AddInfrastructure` for this reason.
- `dotnet ef migrations add <Name>` would diff against an empty snapshot, so do **not**
  use it on top of the hand-written files.

## Regenerating a canonical history (recommended once, on a machine with NuGet)

```bash
./scripts/regenerate-migrations.sh
```

The script deletes the hand-written files, runs `dotnet ef migrations add InitialCreate`
(which also writes the snapshot), and re-adds the trigram search index. From then on use
the normal workflow:

```bash
dotnet ef migrations add AddSomething \
  --project api/src/GameLog.Infrastructure --startup-project api/src/GameLog.Api
```

## Migration history

| Migration | Contents |
|-----------|----------|
| `InitialCreate` | Users, Genres, Platforms, Games, GameGenres, GamePlatforms, trigram title index |
