# API contract (`/api/v1`)

Conventions: JSON bodies, camelCase, RFC 7807 errors with a `code` extension, page-based
pagination (`page`, `pageSize`, capped at 50) returning `{ items, page, pageSize, total }`.
Unsafe methods require the `X-CSRF-TOKEN` header from `GET /auth/csrf`.
Interactive documentation: Swagger UI at `/swagger` (Development).

| Area | Endpoints |
|------|-----------|
| Auth | `POST /auth/register` · `POST /auth/login` · `POST /auth/logout` · `GET /auth/csrf` |
| Users | `GET /users/me` · `PATCH /users/me` · `PUT /users/me/avatar` · `POST /users/me/password` · `GET /users/{username}` |
| Follows | `PUT /users/{username}/follow` · `DELETE /users/{username}/follow` · `GET /users/{username}/followers` · `GET /users/{username}/following` |
| Games | `GET /games` · `GET /games/{slug}` · `GET /games/{slug}/related` · `GET /genres` · `GET /platforms` |
| Admin | `POST /admin/games/import` |
| Diary | `GET /users/{username}/diary` · `PUT /diary/{gameId}` · `DELETE /diary/{gameId}` · `POST /diary/{gameId}/playthroughs` · `PATCH/DELETE /playthroughs/{id}` |
| Ratings | `PUT /games/{gameId}/rating` · `DELETE /games/{gameId}/rating` · `GET /games/{gameId}/rating-summary` |
| Reviews | `GET /games/{gameId}/reviews` · `POST /games/{gameId}/reviews` · `GET /reviews/{id}` · `PATCH/DELETE /reviews/{id}` · `PUT/DELETE /reviews/{id}/like` |
| Comments | `GET/POST /games/{gameId}/comments` · `GET/POST /reviews/{id}/comments` · `POST /comments/{id}/replies` · `PATCH/DELETE /comments/{id}` · `PUT/DELETE /comments/{id}/like` |
| Collections | `GET /users/{username}/collections` · `POST /collections` · `GET/PATCH/DELETE /collections/{id}` · `PUT /collections/{id}/games` · `PUT /collections/{id}/cover` |
| Feed | `GET /feed` |
| Notifications | `GET /notifications` · `GET /notifications/unread-count` · `POST /notifications/{id}/read` · `POST /notifications/read-all` |
| Discovery | `GET /discover/popular` · `top-rated` · `new-releases` · `most-completed` · `trending` · `GET /discover/recommendations` |
| Statistics | `GET /users/{username}/stats` · `GET /users/{username}/stats/{year}` · `GET /users/{username}/retrospective/{year}` |
| Media | `GET /media/{*key}` |

## Security rules

- Ownership is derived from the authenticated principal, never from the request body.
- Private collections return `404` to everyone except the owner.
- Spoiler text is withheld by the server unless the viewer opted in or passes `reveal=true`.
