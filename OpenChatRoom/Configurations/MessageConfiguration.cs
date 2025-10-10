using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Channel).WithMany(e => e.Messages)
        .HasForeignKey(e => e.ChannelId).IsRequired();
        builder.HasOne(e => e.Author).WithMany(e => e.Messages)
        .HasForeignKey(e => e.AuthorId);
    }
}
