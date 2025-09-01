using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameLog.Infrastructure.Persistence.Configurations;

public class GamingDiaryEntryConfiguration : IEntityTypeConfiguration<GamingDiaryEntry>
{
    public void Configure(EntityTypeBuilder<GamingDiaryEntry> b)
    {
        b.ToTable("GamingDiaryEntries");
        b.HasKey(e => e.Id);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(e => e.Notes).HasMaxLength(2000);

        b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Game).WithMany().HasForeignKey(e => e.GameId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Platform).WithMany().HasForeignKey(e => e.PlatformId).OnDelete(DeleteBehavior.SetNull);

        // One diary entry per game; repeat runs live in Playthroughs.
        b.HasIndex(e => new { e.UserId, e.GameId }).IsUnique();
        b.HasIndex(e => new { e.UserId, e.Status });
        b.HasIndex(e => new { e.GameId, e.Status });
    }
}

public class PlaythroughConfiguration : IEntityTypeConfiguration<Playthrough>
{
    public void Configure(EntityTypeBuilder<Playthrough> b)
    {
        b.ToTable("Playthroughs", t => t.HasCheckConstraint("CK_Playthroughs_Dates", "\"CompletedOn\" IS NULL OR \"StartedOn\" IS NULL OR \"CompletedOn\" >= \"StartedOn\""));
        b.HasKey(p => p.Id);
        b.Property(p => p.Notes).HasMaxLength(2000);

        b.HasOne(p => p.DiaryEntry).WithMany(e => e.Playthroughs).HasForeignKey(p => p.DiaryEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.Platform).WithMany().HasForeignKey(p => p.PlatformId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(p => new { p.DiaryEntryId, p.Number }).IsUnique();
        b.HasIndex(p => p.CompletedOn);
    }
}
