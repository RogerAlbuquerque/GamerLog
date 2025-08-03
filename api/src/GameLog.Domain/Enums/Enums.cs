namespace GameLog.Domain.Enums;

public enum DiaryStatus
{
    WantToPlay,
    Playing,
    Completed,
    Abandoned,
    OnHold,
}

public enum Difficulty
{
    VeryEasy,
    Easy,
    Medium,
    Hard,
    VeryHard,
}

public enum Visibility
{
    Public,
    Private,
}

public enum UserRole
{
    User,
    Admin,
}

public enum ActivityType
{
    GameCompleted,
    ReviewPublished,
    GameRated,
    CollectionCreated,
    GameAddedToDiary,
}

public enum NotificationType
{
    NewFollower,
    ReviewLiked,
    CommentLiked,
    CommentReply,
    ReviewComment,
}
