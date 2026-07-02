using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PongChampions.Data.Entities;

namespace PongChampions.Data.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(32);
        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasOne(p => p.User).WithOne().HasForeignKey<Player>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
