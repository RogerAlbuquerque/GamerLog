namespace GameLog.Domain.Entities;

public class Game
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Synopsis { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public string? CoverKey { get; set; }
    public string? BackgroundKey { get; set; }
    public string? OfficialUrl { get; set; }
    public List<string> GameModes { get; set; } = [];
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }

    /// <summary>Number of ratings. Maintained with atomic updates (ADR 0002).</summary>
    public int RatingCount { get; set; }
    /// <summary>Sum of all ratings in half-stars (1-10 each).</summary>
    public int RatingSum { get; set; }
    /// <summary>Number of users whose diary entry is currently Completed.</summary>
    public int CompletedCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<GameGenre> GameGenres { get; set; } = [];
    public List<GamePlatform> GamePlatforms { get; set; } = [];

    /// <summary>Average in stars (0.5 steps of precision), or null when unrated.</summary>
    public double? AverageRating => RatingCount == 0 ? null : Math.Round(RatingSum / 2.0 / RatingCount, 2);
}

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}

public class Platform
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}

public class GameGenre
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}

public class GamePlatform
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int PlatformId { get; set; }
    public Platform Platform { get; set; } = null!;
}
