using GameLog.Domain.Enums;

namespace GameLog.Domain.Entities;

public class GameCollection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? CoverKey { get; set; }
    public Visibility Visibility { get; set; } = Visibility.Public;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CollectionGame> Games { get; set; } = [];
}

public class CollectionGame
{
    public Guid CollectionId { get; set; }
    public GameCollection Collection { get; set; } = null!;
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;
    /// <summary>0-based position inside the collection.</summary>
    public int Position { get; set; }
    public DateTime AddedAt { get; set; }
}
