using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PongChampions.Data.Entities;

namespace PongChampions.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Username).IsRequired().HasMaxLength(32);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(32);
        builder.Property(u => u.Country).HasMaxLength(64);
        builder.Property(u=> u.Role).HasConversion<string>().HasMaxLength(32);
    }
}
