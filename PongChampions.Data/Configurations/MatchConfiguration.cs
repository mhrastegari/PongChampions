using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PongChampions.Data.Entities;

namespace PongChampions.Data.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.Property(m => m.Player1Score).IsRequired();
        builder.Property(m => m.Player2Score).IsRequired();
        builder.HasOne<Room>().WithMany().HasForeignKey(m => m.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>().WithMany().HasForeignKey(m => m.Player1Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>().WithMany().HasForeignKey(m => m.Player2Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>().WithMany().HasForeignKey(m => m.WinnerPlayerId).OnDelete(DeleteBehavior.Restrict);
    }
}
