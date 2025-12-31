# Changelog

Format follows [Keep a Changelog](https://keepachangelog.com/). The project has not been released yet, so
everything below is part of the initial development line.

## [Unreleased]

### Added
- Accounts, profiles, avatars, spoiler preference, password change, account deletion.
- Game catalog with trigram search, filters, sorting, game pages; IGDB import for administrators.
- Gaming diary with status and multiple playthroughs per game.
- Half-star ratings with community histogram; structured reviews with spoiler flags.
- Threaded comments, likes, follows, activity feed, notifications.
- Public/private ordered collections with covers.
- Statistics, discovery rails, rule-based recommendations, yearly retrospective with a shareable card.
- Security: hashed passwords, cookie sessions with security stamp, antiforgery, CORS allow-list, rate limiting,
  validated and re-encoded uploads, lockout after repeated failed sign-ins.
- Docker Compose stack (PostgreSQL, MinIO, API, web), `.env.example`, CI workflow, documentation and ADRs.

### Known issues
- Backend not yet compiled against the real NuGet packages; migrations have no model snapshot.
  See [docs/validation.md](docs/validation.md).
