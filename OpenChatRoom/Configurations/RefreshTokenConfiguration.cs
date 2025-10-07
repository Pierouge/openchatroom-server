using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(e => new { e.Token, e.UserId });
        builder.HasOne(e => e.User).WithOne(u => u.refreshToken)
            .HasForeignKey<RefreshToken>(e => e.UserId);
    }
}
