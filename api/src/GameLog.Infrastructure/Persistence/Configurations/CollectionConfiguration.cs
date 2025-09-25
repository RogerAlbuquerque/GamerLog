using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameLog.Infrastructure.Persistence.Configurations;

public class GameCollectionConfiguration : IEntityTypeConfiguration<GameCollection>
{
    public void Configure(EntityTypeBuilder<GameCollection> b)
    {
        b.ToTable("GameCollections");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever(); // client-generated v7 GUID
        b.Property(c => c.Name).HasMaxLength(100).IsRequired();
        b.Property(c => c.Description).HasMaxLength(1000);
        b.Property(c => c.CoverKey).HasMaxLength(200);
        b.Property(c => c.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(c => new { c.UserId, c.CreatedAt });
    }
}

public class CollectionGameConfiguration : IEntityTypeConfiguration<CollectionGame>
{
    public void Configure(EntityTypeBuilder<CollectionGame> b)
    {
        b.ToTable("CollectionGames");
        b.HasKey(x => new { x.CollectionId, x.GameId });
        b.HasOne(x => x.Collection).WithMany(c => c.Games).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CollectionId, x.Position }).IsUnique();
        b.HasIndex(x => x.GameId);
    }
}
