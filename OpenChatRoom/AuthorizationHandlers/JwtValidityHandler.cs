using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

public sealed class JwtValidityRequirement : IAuthorizationRequirement
{
  public string RequiredAuthValue { get; } = "true";
}

public class JwtValidityHandler(UserAccessor userAccessor) : AuthorizationHandler<JwtValidityRequirement>
{
  private readonly UserAccessor userAccessor = userAccessor;

  protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, JwtValidityRequirement requirement)
  {
    User? user = userAccessor.GetCurrentUser();
    if (user == null) return;

    // Ensure auth = true
    string? authClaim = context.User.FindFirst("auth")?.Value;
    if (string.IsNullOrWhiteSpace(authClaim) || authClaim != requirement.RequiredAuthValue) return;

    // Ensure token is younger or same age as LastToken
    string? iatString = context.User.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
    if (!long.TryParse(iatString, out long iatSeconds)) return;

    DateTime tokenTime = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;

    if (tokenTime >= user.LastToken) context.Succeed(requirement);
  }
}
