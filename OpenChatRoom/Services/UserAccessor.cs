using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

public class UserAccessor(AppDbContext db)
{
  private readonly AppDbContext _db = db;

  public User? GetCurrentUser(HttpContext context)
  {
    string? userId = context.User
        .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
      return null;

    return _db.Users.FirstOrDefault(u => u.Id == userId);
  }

  public User? GetCurrentUser(AuthorizationHandlerContext context)
  {
    string? userId = context.User
        .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
      return null;

    return _db.Users.FirstOrDefault(u => u.Id == userId);
  }

}
