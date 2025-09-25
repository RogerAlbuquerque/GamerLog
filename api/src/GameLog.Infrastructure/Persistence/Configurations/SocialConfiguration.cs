using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameLog.Infrastructure.Persistence.Configurations;

public class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> b)
    {
        b.ToTable("UserFollows", t => t.HasCheckConstraint("CK_UserFollows_NotSelf", "\"FollowerId\" <> \"FolloweeId\""));
        b.HasKey(f => new { f.FollowerId, f.FolloweeId });
        b.HasOne(f => f.Follower).WithMany().HasForeignKey(f => f.FollowerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(f => f.Followee).WithMany().HasForeignKey(f => f.FolloweeId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(f => f.FolloweeId);
    }
}

public class ActivityFeedItemConfiguration : IEntityTypeConfiguration<ActivityFeedItem>
{
    public void Configure(EntityTypeBuilder<ActivityFeedItem> b)
    {
        b.ToTable("ActivityFeed");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).ValueGeneratedNever(); // client-generated v7 GUID
        b.Property(a => a.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.HasOne(a => a.Actor).WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.Game).WithMany().HasForeignKey(a => a.GameId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(a => new { a.ActorId, a.CreatedAt });
        b.HasIndex(a => a.CreatedAt);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(n => n.Id);
        b.Property(n => n.Id).ValueGeneratedNever(); // client-generated v7 GUID
        b.Property(n => n.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.HasOne(n => n.Recipient).WithMany().HasForeignKey(n => n.RecipientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(n => n.Actor).WithMany().HasForeignKey(n => n.ActorId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(n => new { n.RecipientId, n.ReadAt, n.CreatedAt });
    }
}
