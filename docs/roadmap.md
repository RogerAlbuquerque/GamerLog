# Development roadmap

1. **Foundation** — solution layout, Docker Compose (PostgreSQL, MinIO), Next.js shell,
   configuration, Swagger, logging.
2. **Core** — authentication, profiles, avatars, game catalog and search, IGDB import,
   gaming diary with playthroughs, ratings, reviews.
3. **Social** — comments, replies, likes, follows, activity feed, notifications.
4. **Advanced** — collections, statistics, discovery, rule-based recommendations,
   spoiler protection, yearly retrospective with shareable cards.
5. **Quality** — unit and integration tests, authorization tests, frontend tests,
   security review, performance indexes, documentation.

Each phase keeps the project runnable and ends only when its features work
end-to-end: UI, API, business logic and persistence.

## Status

All five phases are implemented. What remains is verification that needs network access: compiling and testing
the backend against the real NuGet packages, running the integration tests, regenerating the migration history with
`scripts/regenerate-migrations.sh`, and an end-to-end run with `docker compose up --build`
(see [validation.md](validation.md)).

Ideas beyond the original scope: email verification and password reset, an outbox for notifications, per-user
privacy settings for the diary, import of play history from other services, and a moderation queue.
