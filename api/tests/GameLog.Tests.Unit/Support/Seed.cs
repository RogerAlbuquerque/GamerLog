using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Infrastructure.Persistence;

namespace GameLog.Tests.Unit.Support;

/// <summary>Terse builders so each test states only what it cares about.</summary>
public static class Seed
{
    public static readonly DateTime Now = new(2025, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    public static User User(AppDbContext db, string username = "roger")
    {
        var user = new User
        {
            Username = username,
            UsernameNormalized = username.ToLowerInvariant(),
            Email = $"{username}@example.com",
            EmailNormalized = $"{username}@example.com".ToLowerInvariant(),
            DisplayName = username,
            PasswordHash = "x",
            CreatedAt = Now,
        };
        db.Set<User>().Add(user);
        db.SaveChanges();
        return user;
    }

    public static Game Game(
        AppDbContext db, string title, DateOnly? release = null, int ratingSum = 0, int ratingCount = 0,
        int completed = 0, string[]? genres = null, string[]? platforms = null)
    {
        var game = new Game
        {
            Title = title,
            Slug = SlugGenerator.From(title),
            ReleaseDate = release,
            RatingSum = ratingSum,
            RatingCount = ratingCount,
            CompletedCount = completed,
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        foreach (var name in genres ?? [])
        {
            game.GameGenres.Add(new GameGenre { Genre = Genre(db, name) });
        }

        foreach (var name in platforms ?? [])
        {
            game.GamePlatforms.Add(new GamePlatform { Platform = Platform(db, name) });
        }

        db.Set<Game>().Add(game);
        db.SaveChanges();
        return game;
    }

    public static Genre Genre(AppDbContext db, string name)
    {
        var slug = SlugGenerator.From(name);
        var existing = db.Set<Genre>().FirstOrDefault(g => g.Slug == slug);
        if (existing is not null)
        {
            return existing;
        }

        var genre = new Genre { Name = name, Slug = slug };
        db.Set<Genre>().Add(genre);
        db.SaveChanges();
        return genre;
    }

    public static Platform Platform(AppDbContext db, string name)
    {
        var slug = SlugGenerator.From(name);
        var existing = db.Set<Platform>().FirstOrDefault(p => p.Slug == slug);
        if (existing is not null)
        {
            return existing;
        }

        var platform = new Platform { Name = name, Slug = slug };
        db.Set<Platform>().Add(platform);
        db.SaveChanges();
        return platform;
    }
}
