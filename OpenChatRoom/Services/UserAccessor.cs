using System.IdentityModel.Tokens.Jwt;

public class UserAccessor
{
  private readonly AppDbContext _db;
  private readonly IHttpContextAccessor _http;

  public UserAccessor(AppDbContext db, IHttpContextAccessor http)
  {
    _db = db;
    _http = http;
  }

  public User? GetCurrentUser()
  {
    var userId = _http.HttpContext?.User
        .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
      return null;

    return _db.Users.FirstOrDefault(u => u.Id == userId);
  }
}
