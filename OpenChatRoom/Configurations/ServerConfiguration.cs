using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.ToTable("servers");
        builder.HasKey(u => u.Id);
        builder.HasMany(e => e.Members).WithMany(u => u.Servers)
        .UsingEntity("server_members",
        r => r.HasOne(typeof(User)).WithMany().HasForeignKey("user").HasPrincipalKey(nameof(User.Id)),
        l => l.HasOne(typeof(Server)).WithMany().HasForeignKey("server").HasPrincipalKey(nameof(Server.Id)),
        j => j.HasKey("user", "server")
        );
    }
}