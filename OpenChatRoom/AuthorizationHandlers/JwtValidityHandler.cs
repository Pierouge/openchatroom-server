using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

public sealed class JwtValidityRequirement : IAuthorizationRequirement
{
  public string RequiredAuthValue { get; } = "true";
}

public class JwtValidityHandler(UserAccessor accessor, AppDbContext dbContext) : AuthorizationHandler<JwtValidityRequirement>
{
  private readonly UserAccessor _accessor = accessor;
  private readonly AppDbContext _dbContext = dbContext;

  protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, JwtValidityRequirement requirement)
  {
    // Get user manually, as UserAccessor relies on dependencies not yet built at that point
    User? user = _accessor.GetCurrentUser(context);
    if (user == null) return;

    // Ensure auth = true
    string? authClaim = context.User.FindFirst("auth")?.Value;
    if (string.IsNullOrWhiteSpace(authClaim) || authClaim != requirement.RequiredAuthValue) return;

    // Ensure token is registered
    string? jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    if (string.IsNullOrWhiteSpace(jti)) return;

    UserToken? userToken = _dbContext.UserTokens.FirstOrDefault(t => t.Id == jti);
    if (userToken != null) context.Succeed(requirement);
  }
}
