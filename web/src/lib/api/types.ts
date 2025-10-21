// Hand-written mirror of the API contracts (the *Contracts.cs files under api/src/GameLog.Application). Enums travel as strings.

export type DiaryStatus = "WantToPlay" | "Playing" | "Completed" | "Abandoned" | "OnHold";
export type Difficulty = "VeryEasy" | "Easy" | "Medium" | "Hard" | "VeryHard";
export type Visibility = "Public" | "Private";
export type UserRole = "User" | "Admin";
export type ActivityType = "GameCompleted" | "ReviewPublished" | "GameRated" | "CollectionCreated" | "GameAddedToDiary";
export type NotificationType = "NewFollower" | "ReviewLiked" | "CommentLiked" | "CommentReply" | "ReviewComment";

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface NamedItem {
  id: number;
  name: string;
  slug: string;
}

export interface SessionUser {
  id: string;
  username: string;
  displayName: string;
  avatarUrl: string | null;
  role: UserRole;
  showSpoilersByDefault: boolean;
}

export interface AuthResponse {
  user: SessionUser;
  csrfToken: string;
}

export interface Profile {
  id: string;
  username: string;
  displayName: string;
  bio: string | null;
  avatarUrl: string | null;
  joinedAt: string;
}

export interface GameSummary {
  id: string;
  slug: string;
  title: string;
  coverUrl: string | null;
  releaseDate: string | null;
  averageRating: number | null;
  ratingCount: number;
  genres: string[];
  platforms: string[];
}

export interface GameDetail {
  id: string;
  slug: string;
  title: string;
  synopsis: string | null;
  coverUrl: string | null;
  backgroundUrl: string | null;
  releaseDate: string | null;
  developer: string | null;
  publisher: string | null;
  officialUrl: string | null;
  gameModes: string[];
  genres: NamedItem[];
  platforms: NamedItem[];
  averageRating: number | null;
  ratingCount: number;
  completedCount: number;
}

export interface GameSearchParams {
  q?: string;
  genre?: string;
  platform?: string;
  year?: number;
  sort?: "title" | "release" | "rating" | "popular";
  page?: number;
  pageSize?: number;
}

export interface Playthrough {
  id: string;
  number: number;
  startedOn: string | null;
  completedOn: string | null;
  playtimeMinutes: number;
  platformId: number | null;
  platform: string | null;
  notes: string | null;
}

export interface DiaryEntry {
  id: string;
  gameId: string;
  gameSlug: string;
  gameTitle: string;
  coverUrl: string | null;
  status: DiaryStatus;
  platformId: number | null;
  platform: string | null;
  totalPlaytimeMinutes: number;
  ratingHalfStars: number | null;
  recommended: boolean | null;
  notes: string | null;
  startedOn: string | null;
  completedOn: string | null;
  createdAt: string;
  updatedAt: string;
  playthroughs: Playthrough[];
}

export interface UpsertDiaryRequest {
  status: DiaryStatus;
  platformId: number | null;
  startedOn: string | null;
  completedOn: string | null;
  playtimeMinutes: number | null;
  recommended: boolean | null;
  notes: string | null;
}

export interface PlaythroughRequest {
  startedOn: string | null;
  completedOn: string | null;
  playtimeMinutes: number | null;
  platformId: number | null;
  notes: string | null;
}

export interface Rating {
  gameId: string;
  halfStars: number;
  stars: number;
  updatedAt: string;
}

export interface RatingSummary {
  gameId: string;
  average: number | null;
  count: number;
  distribution: number[];
}

export interface Author {
  id: string;
  username: string;
  displayName: string;
  avatarUrl: string | null;
}

export interface Review {
  id: string;
  gameId: string;
  gameSlug: string;
  gameTitle: string;
  coverUrl: string | null;
  author: Author;
  title: string;
  body: string;
  experience: string | null;
  pros: string | null;
  cons: string | null;
  favoriteCharacter: string | null;
  favoriteMoment: string | null;
  difficulty: Difficulty | null;
  playtimeMinutes: number | null;
  platformId: number | null;
  platform: string | null;
  completedOn: string | null;
  containsSpoilers: boolean;
  ratingHalfStars: number | null;
  likeCount: number;
  commentCount: number;
  likedByMe: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SaveReviewRequest {
  title: string;
  body: string;
  containsSpoilers: boolean;
  halfStars: number | null;
  experience: string | null;
  pros: string | null;
  cons: string | null;
  favoriteCharacter: string | null;
  favoriteMoment: string | null;
  difficulty: Difficulty | null;
  playtimeMinutes: number | null;
  platformId: number | null;
  completedOn: string | null;
}

export interface LikeResponse {
  liked: boolean;
  likeCount: number;
}

export interface Comment {
  id: string;
  parentId: string | null;
  author: Author | null;
  body: string;
  isSpoiler: boolean;
  isDeleted: boolean;
  likeCount: number;
  replyCount: number;
  likedByMe: boolean;
  createdAt: string;
  editedAt: string | null;
}

export interface CollectionSummary {
  id: string;
  owner: Author;
  name: string;
  description: string | null;
  coverUrl: string | null;
  visibility: Visibility;
  gameCount: number;
  previewCovers: string[];
  createdAt: string;
  updatedAt: string;
}

export interface CollectionGame {
  gameId: string;
  slug: string;
  title: string;
  coverUrl: string | null;
  releaseYear: number | null;
  position: number;
}

export interface CollectionDetail {
  collection: CollectionSummary;
  games: CollectionGame[];
}

export interface SaveCollectionRequest {
  name: string;
  description: string | null;
  visibility: Visibility;
}

export interface FollowState {
  following: boolean;
  followers: number;
  followingCount: number;
}

export interface UserListItem {
  id: string;
  username: string;
  displayName: string;
  avatarUrl: string | null;
  isFollowedByMe: boolean;
}

export interface Activity {
  id: string;
  type: ActivityType;
  actor: Author;
  game: { id: string; slug: string; title: string; coverUrl: string | null } | null;
  reviewId: string | null;
  reviewTitle: string | null;
  collectionId: string | null;
  collectionName: string | null;
  ratingHalfStars: number | null;
  createdAt: string;
}

export interface AppNotification {
  id: string;
  type: NotificationType;
  actor: Author;
  gameId: string | null;
  gameSlug: string | null;
  gameTitle: string | null;
  reviewId: string | null;
  commentId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NamedCount {
  name: string;
  count: number;
}

export interface GameStat {
  gameId: string;
  slug: string;
  title: string;
  coverUrl: string | null;
  playtimeMinutes: number;
  halfStars: number | null;
}

export interface Statistics {
  totalGames: number;
  byStatus: Record<DiaryStatus, number>;
  completedGames: number;
  totalPlaytimeMinutes: number;
  averageRating: number | null;
  ratingCount: number;
  ratingDistribution: number[];
  topGenres: NamedCount[];
  topPlatforms: NamedCount[];
  mostPlayed: GameStat[];
  highestRated: GameStat[];
  reviewCount: number;
  collectionCount: number;
}

export interface RecapGame {
  gameId: string;
  slug: string;
  title: string;
  coverUrl: string | null;
  playtimeMinutes: number;
  halfStars: number | null;
  completedOn: string | null;
}

export interface YearRecap {
  year: number;
  gamesCompleted: number;
  completions: number;
  totalPlaytimeMinutes: number;
  averageRating: number | null;
  completedByMonth: number[];
  busiestMonth: number | null;
  topGenres: NamedCount[];
  topPlatforms: NamedCount[];
  longestGame: RecapGame | null;
  shortestGame: RecapGame | null;
  firstCompleted: RecapGame | null;
  lastCompleted: RecapGame | null;
  highestRated: RecapGame[];
  reviewsWritten: number;
  ratingsGiven: number;
  availableYears: number[];
}

export interface Discovery {
  popular: GameSummary[];
  topRated: GameSummary[];
  newReleases: GameSummary[];
  mostCompleted: GameSummary[];
  trending: GameSummary[];
}

export interface RecommendedGame {
  game: GameSummary;
  score: number;
  because: string[];
}

/** RFC 7807 problem details as returned by the API; `errors` holds per-field messages for validation failures. */
export interface Problem {
  status: number;
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}
