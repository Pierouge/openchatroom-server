using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
  public void Configure(EntityTypeBuilder<UserToken> builder)
  {
    builder.ToTable("user_tokens");
    builder.HasKey(e => e.Id);
    builder.HasOne(e => e.User).WithMany(e => e.UserTokens)
    .HasForeignKey(e => e.UserId);
  }
}
