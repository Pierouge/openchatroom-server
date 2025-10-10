using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("channels");
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Server).WithMany(e => e.Channels)
        .HasForeignKey(e => e.ServerId);
        builder.HasMany(e => e.PrivateChannelMembers).WithMany(e => e.PrivateChannels)
        .UsingEntity("private_channel_members",
        r => r.HasOne(typeof(User)).WithMany().HasForeignKey("user").HasPrincipalKey(nameof(User.Id)),
        l => l.HasOne(typeof(Channel)).WithMany().HasForeignKey("channel").HasPrincipalKey(nameof(Channel.Id)),
        j => j.HasKey("user", "channel")
        );
    }
}
