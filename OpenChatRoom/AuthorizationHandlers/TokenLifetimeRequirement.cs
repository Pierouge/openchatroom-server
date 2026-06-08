using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class TokenLifetimeRequirement : IAuthorizationRequirement
{
}

public class TokenLifetimeHandler : AuthorizationHandler<TokenLifetimeRequirement>
{
  protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TokenLifetimeRequirement requirement)
  {
    HttpContext? http = TryGetHttpContext(context);

    var expClaim = context.User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
    if (!string.IsNullOrWhiteSpace(expClaim) && long.TryParse(expClaim, out long exp))
    {
      var expirationTime = UnixTimeStampToDateTime(exp);
      if (DateTime.UtcNow > expirationTime)
      {
        http?.Items["ErrorMessage"] = "Token has expired";
        context.Fail();
        return Task.CompletedTask;
      }
    }

    context.Succeed(requirement);
    return Task.CompletedTask;
  }

  private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
  {
    var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
    return dateTime;
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
