# Contributing

## Workflow

1. Branch from `main`; keep changes focused.
2. Write [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `test:`, `docs:`,
   `build:`, `ci:`, `refactor:`, `chore:`.
3. Add or update tests with the change. Pure logic goes in pure functions (see the statistics and recommender
   calculators) so it can be tested without a database.
4. Before opening a pull request run the checks from [docs/testing.md](docs/testing.md).

## Conventions

- **API:** controllers stay thin; rules live in `Domain/Rules`, use cases in `Application/*Service`. Expected
  failures are `AppException` subclasses (they become RFC 7807 responses). Never take a user id from a request body.
- **Database:** change the model and add a migration in the same commit. Counters are recomputed with SQL, not in memory.
- **Web:** route files stay thin, views are client components, all HTTP goes through `lib/api`. Render user text as
  text (`Prose`); never use `dangerouslySetInnerHTML`.
- **Security-sensitive changes** (auth, uploads, authorization) update [docs/security.md](docs/security.md).
- No secrets, real credentials or personal data in the repository or in test fixtures.
