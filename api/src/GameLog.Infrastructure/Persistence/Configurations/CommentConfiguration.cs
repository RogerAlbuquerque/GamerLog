using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameLog.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> b)
    {
        b.ToTable("Comments", t => t.HasCheckConstraint(
            "CK_Comments_OneTarget",
            "(\"GameId\" IS NOT NULL AND \"ReviewId\" IS NULL) OR (\"GameId\" IS NULL AND \"ReviewId\" IS NOT NULL)"));
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever(); // client-generated v7 GUID
        b.Property(c => c.Body).HasMaxLength(2000).IsRequired();

        b.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Game>().WithMany().HasForeignKey(c => c.GameId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Review>().WithMany().HasForeignKey(c => c.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Comment>().WithMany().HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(c => new { c.GameId, c.CreatedAt });
        b.HasIndex(c => new { c.GameId, c.LikeCount });
        b.HasIndex(c => new { c.ReviewId, c.CreatedAt });
        b.HasIndex(c => new { c.ParentCommentId, c.CreatedAt });
        b.HasIndex(c => c.UserId);
    }
}

public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> b)
    {
        b.ToTable("CommentLikes");
        b.HasKey(l => new { l.CommentId, l.UserId });
        b.HasOne(l => l.Comment).WithMany().HasForeignKey(l => l.CommentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(l => l.UserId);
    }
}
