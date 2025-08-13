using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameLog.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> b)
    {
        b.ToTable("Games");
        b.HasKey(g => g.Id);
        b.Property(g => g.Slug).HasMaxLength(100).IsRequired();
        b.Property(g => g.Title).HasMaxLength(200).IsRequired();
        b.Property(g => g.Synopsis).HasMaxLength(4000);
        b.Property(g => g.Developer).HasMaxLength(200);
        b.Property(g => g.Publisher).HasMaxLength(200);
        b.Property(g => g.CoverKey).HasMaxLength(200);
        b.Property(g => g.BackgroundKey).HasMaxLength(200);
        b.Property(g => g.OfficialUrl).HasMaxLength(500);
        b.Property(g => g.ExternalProvider).HasMaxLength(30);
        b.Property(g => g.ExternalId).HasMaxLength(50);

        // Stored as a single delimited column so every provider (and the test database) behaves the same.
        b.Property(g => g.GameModes)
            .HasMaxLength(300)
            .HasConversion(
                v => string.Join('|', v),
                v => v.Length == 0 ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new ValueComparer<List<string>>(
                    (x, y) => x!.SequenceEqual(y!),
                    v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                    v => v.ToList()));

        b.Ignore(g => g.AverageRating);
        b.HasIndex(g => g.Slug).IsUnique();
        b.HasIndex(g => new { g.ExternalProvider, g.ExternalId }).IsUnique();
        b.HasIndex(g => g.ReleaseDate);
        b.HasIndex(g => g.CompletedCount);
        b.HasIndex(g => g.RatingCount);
    }
}

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> b)
    {
        b.ToTable("Genres");
        b.HasKey(g => g.Id);
        b.Property(g => g.Name).HasMaxLength(60).IsRequired();
        b.Property(g => g.Slug).HasMaxLength(60).IsRequired();
        b.HasIndex(g => g.Slug).IsUnique();
        b.HasIndex(g => g.Name).IsUnique();
    }
}

public class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> b)
    {
        b.ToTable("Platforms");
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).HasMaxLength(60).IsRequired();
        b.Property(p => p.Slug).HasMaxLength(60).IsRequired();
        b.HasIndex(p => p.Slug).IsUnique();
        b.HasIndex(p => p.Name).IsUnique();
    }
}

public class GameGenreConfiguration : IEntityTypeConfiguration<GameGenre>
{
    public void Configure(EntityTypeBuilder<GameGenre> b)
    {
        b.ToTable("GameGenres");
        b.HasKey(x => new { x.GameId, x.GenreId });
        b.HasOne(x => x.Game).WithMany(g => g.GameGenres).HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.GenreId);
    }
}

public class GamePlatformConfiguration : IEntityTypeConfiguration<GamePlatform>
{
    public void Configure(EntityTypeBuilder<GamePlatform> b)
    {
        b.ToTable("GamePlatforms");
        b.HasKey(x => new { x.GameId, x.PlatformId });
        b.HasOne(x => x.Game).WithMany(g => g.GamePlatforms).HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Platform).WithMany().HasForeignKey(x => x.PlatformId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.PlatformId);
    }
}
