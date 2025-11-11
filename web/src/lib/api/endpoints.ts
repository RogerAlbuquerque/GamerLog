import { http } from "./client";
import type {
  Activity, AppNotification, AuthResponse, CollectionDetail, CollectionSummary, Comment, DiaryEntry, DiaryStatus, Discovery,
  FollowState, GameDetail, GameSearchParams, GameSummary, LikeResponse, NamedItem, Paged, PlaythroughRequest, Profile,
  Rating, RatingSummary, RecommendedGame, Review, SaveCollectionRequest, SaveReviewRequest, SessionUser, Statistics,
  UpsertDiaryRequest, UserListItem, YearRecap,
} from "./types";

type PageParams = { page?: number; pageSize?: number };
const enc = encodeURIComponent;

export const authApi = {
  me: () => http.get<SessionUser>("/auth/me"),
  register: (body: { username: string; email: string; password: string; displayName?: string }) =>
    http.post<AuthResponse>("/auth/register", body),
  login: (body: { login: string; password: string }) => http.post<AuthResponse>("/auth/login", body),
  logout: () => http.post<{ token: string }>("/auth/logout"),
  changePassword: (body: { currentPassword: string; newPassword: string }) => http.post<AuthResponse>("/auth/password", body),
  deleteAccount: (password: string) => http.delete("/users/me", { password }),
};

export const gamesApi = {
  search: (params: GameSearchParams) => http.get<Paged<GameSummary>>("/games", { ...params }),
  get: (slug: string) => http.get<GameDetail>(`/games/${enc(slug)}`),
  related: (slug: string) => http.get<GameSummary[]>(`/games/${enc(slug)}/related`),
  genres: () => http.get<NamedItem[]>("/genres"),
  platforms: () => http.get<NamedItem[]>("/platforms"),
};

export const ratingsApi = {
  summary: (gameId: string) => http.get<RatingSummary>(`/games/${gameId}/rating-summary`),
  mine: (gameId: string) => http.get<Rating>(`/games/${gameId}/rating`),
  set: (gameId: string, halfStars: number) => http.put<Rating>(`/games/${gameId}/rating`, { halfStars }),
  remove: (gameId: string) => http.delete(`/games/${gameId}/rating`),
};

export const diaryApi = {
  list: (username: string, params: PageParams & { status?: DiaryStatus }) =>
    http.get<Paged<DiaryEntry>>(`/users/${enc(username)}/diary`, { ...params }),
  mine: (gameId: string) => http.get<DiaryEntry>(`/diary/${gameId}`),
  save: (gameId: string, body: UpsertDiaryRequest) => http.put<DiaryEntry>(`/diary/${gameId}`, body),
  remove: (gameId: string) => http.delete(`/diary/${gameId}`),
  addPlaythrough: (gameId: string, body: PlaythroughRequest) => http.post<DiaryEntry>(`/diary/${gameId}/playthroughs`, body),
  updatePlaythrough: (id: string, body: PlaythroughRequest) => http.patch<DiaryEntry>(`/playthroughs/${id}`, body),
  removePlaythrough: (id: string) => http.delete<DiaryEntry>(`/playthroughs/${id}`),
};

export const reviewsApi = {
  forGame: (gameId: string, params: PageParams & { sort?: "newest" | "popular" }) =>
    http.get<Paged<Review>>(`/games/${gameId}/reviews`, { ...params }),
  forUser: (username: string, params: PageParams & { sort?: "newest" | "popular" }) =>
    http.get<Paged<Review>>(`/users/${enc(username)}/reviews`, { ...params }),
  get: (id: string) => http.get<Review>(`/reviews/${id}`),
  mine: (gameId: string) => http.get<Review>(`/games/${gameId}/review`),
  save: (gameId: string, body: SaveReviewRequest) => http.put<Review>(`/games/${gameId}/review`, body),
  remove: (id: string) => http.delete(`/reviews/${id}`),
  like: (id: string) => http.put<LikeResponse>(`/reviews/${id}/like`),
  unlike: (id: string) => http.delete<LikeResponse>(`/reviews/${id}/like`),
};

export const commentsApi = {
  forGame: (gameId: string, params: PageParams & { sort?: "newest" | "top" }) =>
    http.get<Paged<Comment>>(`/games/${gameId}/comments`, { ...params }),
  forReview: (reviewId: string, params: PageParams & { sort?: "newest" | "top" }) =>
    http.get<Paged<Comment>>(`/reviews/${reviewId}/comments`, { ...params }),
  replies: (id: string, params: PageParams = {}) => http.get<Paged<Comment>>(`/comments/${id}/replies`, { ...params }),
  createOnGame: (gameId: string, body: { body: string; isSpoiler: boolean; parentCommentId?: string | null }) =>
    http.post<Comment>(`/games/${gameId}/comments`, body),
  createOnReview: (reviewId: string, body: { body: string; isSpoiler: boolean; parentCommentId?: string | null }) =>
    http.post<Comment>(`/reviews/${reviewId}/comments`, body),
  edit: (id: string, body: { body: string; isSpoiler: boolean }) => http.patch<Comment>(`/comments/${id}`, body),
  remove: (id: string) => http.delete(`/comments/${id}`),
  like: (id: string) => http.put<LikeResponse>(`/comments/${id}/like`),
  unlike: (id: string) => http.delete<LikeResponse>(`/comments/${id}/like`),
};

export const usersApi = {
  profile: (username: string) => http.get<Profile>(`/users/${enc(username)}`),
  update: (body: { displayName?: string; bio?: string; showSpoilersByDefault?: boolean }) => http.patch<SessionUser>("/users/me", body),
  setAvatar: (form: FormData) => http.upload<{ avatarUrl: string | null }>("/users/me/avatar", form),
  removeAvatar: () => http.delete("/users/me/avatar"),
  followState: (username: string) => http.get<FollowState>(`/users/${enc(username)}/follow`),
  follow: (username: string) => http.put<FollowState>(`/users/${enc(username)}/follow`),
  unfollow: (username: string) => http.delete<FollowState>(`/users/${enc(username)}/follow`),
  followers: (username: string, params: PageParams = {}) => http.get<Paged<UserListItem>>(`/users/${enc(username)}/followers`, { ...params }),
  following: (username: string, params: PageParams = {}) => http.get<Paged<UserListItem>>(`/users/${enc(username)}/following`, { ...params }),
};

export const collectionsApi = {
  listPublic: (params: PageParams = {}) => http.get<Paged<CollectionSummary>>("/collections", { ...params }),
  forUser: (username: string, params: PageParams = {}) => http.get<Paged<CollectionSummary>>(`/users/${enc(username)}/collections`, { ...params }),
  get: (id: string) => http.get<CollectionDetail>(`/collections/${id}`),
  create: (body: SaveCollectionRequest) => http.post<CollectionDetail>("/collections", body),
  update: (id: string, body: SaveCollectionRequest) => http.put<CollectionDetail>(`/collections/${id}`, body),
  remove: (id: string) => http.delete(`/collections/${id}`),
  addGame: (id: string, gameId: string) => http.post<CollectionDetail>(`/collections/${id}/games`, { gameId }),
  removeGame: (id: string, gameId: string) => http.delete<CollectionDetail>(`/collections/${id}/games/${gameId}`),
  reorder: (id: string, gameIds: string[]) => http.put<CollectionDetail>(`/collections/${id}/order`, { gameIds }),
  setCover: (id: string, form: FormData) => http.upload<{ coverUrl: string | null }>(`/collections/${id}/cover`, form),
  removeCover: (id: string) => http.delete(`/collections/${id}/cover`),
};

export const activityApi = {
  feed: (params: PageParams = {}) => http.get<Paged<Activity>>("/feed", { ...params }),
  forUser: (username: string, params: PageParams = {}) => http.get<Paged<Activity>>(`/users/${enc(username)}/activity`, { ...params }),
};

export const notificationsApi = {
  list: (params: PageParams & { unreadOnly?: boolean } = {}) => http.get<Paged<AppNotification>>("/notifications", { ...params }),
  unreadCount: () => http.get<{ count: number }>("/notifications/unread-count"),
  markRead: (id: string) => http.post<void>(`/notifications/${id}/read`),
  markAllRead: () => http.post<void>("/notifications/read-all"),
};

export const statsApi = {
  stats: (username: string) => http.get<Statistics>(`/users/${enc(username)}/stats`),
  recap: (username: string, year?: number) => http.get<YearRecap>(`/users/${enc(username)}/recap`, { year }),
};

export const discoveryApi = {
  overview: () => http.get<Discovery>("/discover"),
  recommended: (take?: number) => http.get<RecommendedGame[]>("/discover/recommended", { take }),
};
