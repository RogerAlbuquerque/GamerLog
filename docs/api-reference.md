# API reference

All routes are under `/api/v1`. Interactive documentation is served by Swagger UI at `/swagger` (Development, or when `Swagger:Enabled=true`). Conventions (errors, pagination, CSRF) are in [api-contract.md](api-contract.md).

Legend: 🔒 requires a signed-in user (the API's fallback policy denies anonymous access unless an endpoint opts out) · 🛡️ requires the Admin role · `public` is open to visitors. Every mutating request (POST/PUT/PATCH/DELETE) must carry the `X-CSRF-TOKEN` header obtained from `GET /api/v1/auth/csrf`.


## Activity

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/feed` | 🔒 |
| GET | `/api/v1/users/{username}/activity` | public |

## AdminGames

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/admin/games/search` | 🛡️ |
| POST | `/api/v1/admin/games/import` | 🛡️ |

## Auth

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/auth/csrf` | public |
| POST | `/api/v1/auth/register` | public |
| POST | `/api/v1/auth/login` | public |
| POST | `/api/v1/auth/logout` | public |
| GET | `/api/v1/auth/me` | 🔒 |
| POST | `/api/v1/auth/password` | 🔒 |

## Collections

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/collections` | public |
| GET | `/api/v1/users/{username}/collections` | public |
| GET | `/api/v1/collections/{id}` | public |
| POST | `/api/v1/collections` | 🔒 |
| PUT | `/api/v1/collections/{id}` | 🔒 |
| DELETE | `/api/v1/collections/{id}` | 🔒 |
| POST | `/api/v1/collections/{id}/games` | 🔒 |
| DELETE | `/api/v1/collections/{id}/games/{gameId}` | 🔒 |
| PUT | `/api/v1/collections/{id}/order` | 🔒 |
| PUT | `/api/v1/collections/{id}/cover` | 🔒 |
| DELETE | `/api/v1/collections/{id}/cover` | 🔒 |

## Comments

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/games/{gameId}/comments` | public |
| GET | `/api/v1/reviews/{reviewId}/comments` | public |
| GET | `/api/v1/comments/{id}/replies` | public |
| POST | `/api/v1/games/{gameId}/comments` | 🔒 |
| POST | `/api/v1/reviews/{reviewId}/comments` | 🔒 |
| PATCH | `/api/v1/comments/{id}` | 🔒 |
| DELETE | `/api/v1/comments/{id}` | 🔒 |
| PUT | `/api/v1/comments/{id}/like` | 🔒 |
| DELETE | `/api/v1/comments/{id}/like` | 🔒 |

## Diary

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/users/{username}/diary` | public |
| GET | `/api/v1/diary/{gameId}` | 🔒 |
| PUT | `/api/v1/diary/{gameId}` | 🔒 |
| DELETE | `/api/v1/diary/{gameId}` | 🔒 |
| POST | `/api/v1/diary/{gameId}/playthroughs` | 🔒 |
| PATCH | `/api/v1/playthroughs/{id}` | 🔒 |
| DELETE | `/api/v1/playthroughs/{id}` | 🔒 |

## Discovery

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/discover` | public |
| GET | `/api/v1/discover/recommended` | 🔒 |

## Follows

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/users/{username}/follow` | public |
| PUT | `/api/v1/users/{username}/follow` | 🔒 |
| DELETE | `/api/v1/users/{username}/follow` | 🔒 |
| GET | `/api/v1/users/{username}/followers` | public |
| GET | `/api/v1/users/{username}/following` | public |

## Games

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/games` | public |
| GET | `/api/v1/games/{slug}` | public |
| GET | `/api/v1/games/{slug}/related` | public |
| GET | `/api/v1/genres` | public |
| GET | `/api/v1/platforms` | public |

## Media

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/media/{**key}` | public |

## Notifications

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/notifications` | 🔒 |
| GET | `/api/v1/notifications/unread-count` | 🔒 |
| POST | `/api/v1/notifications/{id}/read` | 🔒 |
| POST | `/api/v1/notifications/read-all` | 🔒 |

## Ratings

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/games/{gameId}/rating-summary` | public |
| GET | `/api/v1/games/{gameId}/rating` | 🔒 |
| PUT | `/api/v1/games/{gameId}/rating` | 🔒 |
| DELETE | `/api/v1/games/{gameId}/rating` | 🔒 |

## Reviews

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/games/{gameId}/reviews` | public |
| GET | `/api/v1/users/{username}/reviews` | public |
| GET | `/api/v1/reviews/{id}` | public |
| GET | `/api/v1/games/{gameId}/review` | 🔒 |
| PUT | `/api/v1/games/{gameId}/review` | 🔒 |
| DELETE | `/api/v1/reviews/{id}` | 🔒 |
| PUT | `/api/v1/reviews/{id}/like` | 🔒 |
| DELETE | `/api/v1/reviews/{id}/like` | 🔒 |

## Statistics

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/users/{username}/stats` | public |
| GET | `/api/v1/users/{username}/recap` | public |

## Users

| Method | Path | Access |
|---|---|---|
| GET | `/api/v1/users/{username}` | public |
| PATCH | `/api/v1/users/me` | 🔒 |
| PUT | `/api/v1/users/me/avatar` | 🔒 |
| DELETE | `/api/v1/users/me/avatar` | 🔒 |
| DELETE | `/api/v1/users/me` | 🔒 |
