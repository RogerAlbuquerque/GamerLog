using GameLog.Domain.Enums;

namespace GameLog.Domain.Entities;

/// <summary>A user's relationship with a game. Unique per (user, game).</summary>
public class GamingDiaryEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    public DiaryStatus Status { get; set; }
    public int? PlatformId { get; set; }
    public Platform? Platform { get; set; }
    public int TotalPlaytimeMinutes { get; set; }
    public bool? Recommended { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Playthrough> Playthroughs { get; set; } = [];
}

/// <summary>One run through a game; a diary entry may have many.</summary>
public class Playthrough
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DiaryEntryId { get; set; }
    public GamingDiaryEntry DiaryEntry { get; set; } = null!;
    /// <summary>1-based sequence within the diary entry.</summary>
    public int Number { get; set; }
    public DateOnly? StartedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public int PlaytimeMinutes { get; set; }
    public int? PlatformId { get; set; }
    public Platform? Platform { get; set; }
    public string? Notes { get; set; }
}
