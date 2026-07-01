using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PongChampions.Data.Entities;

namespace PongChampions.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasIndex(r => r.Code).IsUnique();
        builder.Property(r => r.Code).HasMaxLength(6);
        builder.HasOne(r => r.HostPlayer).WithMany().HasForeignKey(r => r.HostPlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.GuestPlayer).WithMany().HasForeignKey(r => r.GuestPlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
