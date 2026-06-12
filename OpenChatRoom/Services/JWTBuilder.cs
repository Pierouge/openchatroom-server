using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

public interface IJWTBuilder
{
  // Returns either only the token if isAccess == false, or both the accessToken and the refreshToken
  JWTBuilder.Result GenerateToken(AppDbContext dbContext, string userid, bool isAccess = false, IEnumerable<Claim>? extraClaims = null, TimeSpan? lifetime = null);
}

public class JWTBuilder : IJWTBuilder
{
  private readonly SymmetricSecurityKey signingKey;
  private readonly string authority;
  private readonly string audience;
  private readonly TimeSpan defaultLifetime;
  private readonly TimeSpan defaultRefreshLifetime;

  public JWTBuilder(SymmetricSecurityKey jwtKey, IConfiguration configuration)
  {
    signingKey = jwtKey;
    authority = configuration.GetSection("Jwt")
              .GetValue<string>("Authority")!;
    audience = configuration.GetSection("Jwt")
              .GetValue<string>("Audience")!;

    int minutes = configuration.GetSection("Jwt").GetValue<int>("LifeTimeMinutes");
    defaultLifetime = TimeSpan.FromMinutes(minutes);

    int days = configuration.GetSection("Jwt").GetValue<int>("RefreshLifeTimeDays");
    defaultRefreshLifetime = TimeSpan.FromDays(minutes);
  }

  public Result GenerateToken(AppDbContext dbContext, string userid, bool isAuthenticated = false, IEnumerable<Claim>? extraClaims = null, TimeSpan? lifetime = null)
  {
    DateTime now = DateTime.UtcNow;

    string tokenId = IdGenerator.generateId();
    string refreshTokenId = IdGenerator.generateId();

    List<Claim> claims =
    [
      new(JwtRegisteredClaimNames.Sub, userid),
      new(JwtRegisteredClaimNames.Jti, tokenId),
      new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    ];

    List<Claim> refreshClaims =
    [
      new(JwtRegisteredClaimNames.Sub, userid),
      new(JwtRegisteredClaimNames.Jti, refreshTokenId),
      new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    ];

    if (isAuthenticated)
    {
      claims.Add(new Claim("auth", "true"));

      User user = dbContext.Users.FirstOrDefault(u => u.Id == userid)!;

      UserToken userToken = new(tokenId, refreshTokenId, user, now.Add(lifetime ?? defaultLifetime));
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

    JwtSecurityTokenHandler handler = new();

    if (isAuthenticated)
    {
      JwtSecurityToken refreshToken = new(
          issuer: authority,
          audience: audience,
          claims: refreshClaims,
          notBefore: now,
          expires: now.Add(defaultRefreshLifetime),
          signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
          );
      return new(handler.WriteToken(token), handler.WriteToken(refreshToken));
    }

    return new(handler.WriteToken(token), null);
  }

  public record Result(string Token, string? RefreshToken);
}
