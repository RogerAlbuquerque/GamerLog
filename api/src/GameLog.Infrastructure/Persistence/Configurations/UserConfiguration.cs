using GameLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameLog.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).ValueGeneratedNever(); // client-generated v7 GUID
        b.Property(u => u.Username).HasMaxLength(24).IsRequired();
        b.Property(u => u.UsernameNormalized).HasMaxLength(24).IsRequired();
        b.Property(u => u.Email).HasMaxLength(254).IsRequired();
        b.Property(u => u.EmailNormalized).HasMaxLength(254).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        b.Property(u => u.Bio).HasMaxLength(500);
        b.Property(u => u.AvatarKey).HasMaxLength(200);
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(u => u.UsernameNormalized).IsUnique();
        b.HasIndex(u => u.EmailNormalized).IsUnique();
    }
}
