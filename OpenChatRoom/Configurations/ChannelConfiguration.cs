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
    }
}
