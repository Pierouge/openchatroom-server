using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class FriendsConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("friends");
        builder.HasKey(e => new { e.AuthorId, e.ReceiverId });
        builder.HasOne(e => e.Author).WithMany(e => e.SentRequests)
        .HasForeignKey(e => e.AuthorId);
        builder.HasOne(e => e.Receiver).WithMany(e => e.ReceivedRequests)
        .HasForeignKey(e => e.ReceiverId);
    }
}
