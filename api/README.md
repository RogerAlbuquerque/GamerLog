# GameLog API

ASP.NET Core Web API on .NET 10.

```
src/GameLog.Domain          entities, enums, business rules (no dependencies)
src/GameLog.Application     use cases (*Service), contracts, pure calculators
src/GameLog.Infrastructure  EF Core/PostgreSQL, migrations, S3/filesystem storage, IGDB, image processing
src/GameLog.Api             controllers, middleware, auth/CSRF/CORS/rate limiting, Swagger, composition root
tests/GameLog.Tests.Unit        domain, services, calculators, infrastructure
tests/GameLog.Tests.Integration HTTP pipeline via WebApplicationFactory
```

Dependencies point inwards: Api → Infrastructure → Application → Domain. Services named `*Service` in the
Application assembly are registered automatically.

```bash
dotnet run --project src/GameLog.Api   # http://localhost:5080, Swagger at /swagger
dotnet test GameLog.sln
```

Configuration is read from `appsettings*.json` and environment variables (`Section__Key`). The full list of
settings the stack uses is in `../.env.example` and `../docker-compose.yml`. See `../docs` for architecture,
the API reference and security notes, and `../docs/validation.md` for what has and has not been executed.
