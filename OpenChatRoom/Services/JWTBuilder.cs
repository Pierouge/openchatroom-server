using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

public interface IJWTBuilder
{
  string generateToken(AppDbContext dbContext, string userid, bool isAuthenticated = false, IEnumerable<Claim>? extraClaims = null, TimeSpan? lifetime = null);
}

public class JWTBuilder : IJWTBuilder
{
  private readonly SymmetricSecurityKey signingKey;
  private readonly string authority;
  private readonly string audience;
  private readonly TimeSpan defaultLifetime;

  public JWTBuilder(SymmetricSecurityKey jwtKey, IConfiguration configuration)
  {
    signingKey = jwtKey;
    authority = configuration.GetSection("Jwt")
              .GetValue<string>("Authority")!;
    audience = configuration.GetSection("Jwt")
              .GetValue<string>("Audience")!;

    int days = configuration.GetSection("Jwt").GetValue<int>("LifeTimeDays");
    defaultLifetime = TimeSpan.FromDays(days);
  }

  public string generateToken(AppDbContext dbContext, string userid, bool isAuthenticated = false, IEnumerable<Claim>? extraClaims = null, TimeSpan? lifetime = null)
  {
    DateTime now = DateTime.UtcNow;

    string tokenId = IdGenerator.generateId();

    List<Claim> claims =
    [
      new(JwtRegisteredClaimNames.Sub, userid),
      new(JwtRegisteredClaimNames.Jti, tokenId),
      new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    ];

    if (isAuthenticated)
    {
      claims.Add(new Claim("auth", "true"));

      User user = dbContext.Users.FirstOrDefault(u => u.Id == userid)!;

      UserToken userToken = new(tokenId, user, now);
      dbContext.UserTokens.Add(userToken);
      dbContext.SaveChanges();
    }

    if (extraClaims != null) claims.AddRange(extraClaims);

    JwtSecurityToken token = new(
        issuer: authority,
        audience: audience,
        claims: claims,
        notBefore: now,
        expires: now.Add(lifetime ?? defaultLifetime),
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );
    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
