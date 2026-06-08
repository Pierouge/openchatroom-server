using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

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
    HttpContext? http = TryGetHttpContext(context);

    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
      http?.Items["ErrorMessage"] = "Could not validate the JWT";
      context.Fail();
      return;
    }

    // Get user manually, as UserAccessor relies on dependencies not yet built at that point
    User? user = _accessor.GetCurrentUser(context);
    if (user == null)
    {
      http?.Items["ErrorMessage"] = "User not found";
      context.Fail();
      return;
    }

    // Ensure auth = true
    string? authClaim = context.User.FindFirst("auth")?.Value;
    if (string.IsNullOrWhiteSpace(authClaim) || authClaim != requirement.RequiredAuthValue)
    {
      http?.Items["ErrorMessage"] = "User is not authenticated";
      context.Fail();
      return;
    }

    // Ensure token is registered
    string? jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    if (string.IsNullOrWhiteSpace(jti))
    {
      http?.Items["ErrorMessage"] = "Missing the Jti field in the JWT";
      context.Fail();
      return;
    }

    UserToken? userToken = _dbContext.UserTokens.FirstOrDefault(t => t.Id == jti);
    if (userToken != null) context.Succeed(requirement);
    else
    {
      http?.Items["ErrorMessage"] = "This JWT is not registered";
      context.Fail();
    }
  }

  private static HttpContext? TryGetHttpContext(AuthorizationHandlerContext context)
  {
    if (context.Resource is HttpContext http)
      return http;

    if (context.Resource is AuthorizationFilterContext mvc)
      return mvc.HttpContext;

    return null;
  }

}
