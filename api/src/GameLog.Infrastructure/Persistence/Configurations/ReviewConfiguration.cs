using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameLog.Infrastructure.Persistence.Configurations;

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.ToTable("Ratings", t => t.HasCheckConstraint("CK_Ratings_HalfStars", "\"HalfStars\" BETWEEN 1 AND 10"));
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever(); // client-generated v7 GUID
        b.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.Game).WithMany().HasForeignKey(r => r.GameId).OnDelete(DeleteBehavior.Cascade);

        // The database, not just the service, guarantees one rating per user and game.
        b.HasIndex(r => new { r.UserId, r.GameId }).IsUnique();
        b.HasIndex(r => r.GameId);
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("Reviews");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever(); // client-generated v7 GUID
        b.Property(r => r.Title).HasMaxLength(120).IsRequired();
        b.Property(r => r.Body).HasMaxLength(10000).IsRequired();
        b.Property(r => r.Experience).HasMaxLength(3000);
        b.Property(r => r.Pros).HasMaxLength(1000);
        b.Property(r => r.Cons).HasMaxLength(1000);
        b.Property(r => r.FavoriteCharacter).HasMaxLength(120);
        b.Property(r => r.FavoriteMoment).HasMaxLength(500);
        b.Property(r => r.Difficulty).HasConversion<string>().HasMaxLength(20);

        b.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.Game).WithMany().HasForeignKey(r => r.GameId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.Platform).WithMany().HasForeignKey(r => r.PlatformId).OnDelete(DeleteBehavior.SetNull);

        // A user has one primary review per game.
        b.HasIndex(r => new { r.UserId, r.GameId }).IsUnique();
        b.HasIndex(r => new { r.GameId, r.CreatedAt });
        b.HasIndex(r => new { r.UserId, r.CreatedAt });
    }
}

public class ReviewLikeConfiguration : IEntityTypeConfiguration<ReviewLike>
{
    public void Configure(EntityTypeBuilder<ReviewLike> b)
    {
        b.ToTable("ReviewLikes");
        b.HasKey(l => new { l.ReviewId, l.UserId });
        b.HasOne(l => l.Review).WithMany().HasForeignKey(l => l.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(l => l.UserId);
    }
}
