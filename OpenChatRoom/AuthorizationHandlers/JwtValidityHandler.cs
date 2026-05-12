using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

public sealed class JwtValidityRequirement : IAuthorizationRequirement
{
  public string RequiredAuthValue { get; } = "true";
}

public class JwtValidityHandler(UserAccessor accessor) : AuthorizationHandler<JwtValidityRequirement>
{
  private readonly UserAccessor _accessor = accessor;

  protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, JwtValidityRequirement requirement)
  {
    // Get user manually, as UserAccessor relies on dependencies not yet built at that point
    User? user = _accessor.GetCurrentUser(context);
    if (user == null) return;

    // Ensure auth = true
    string? authClaim = context.User.FindFirst("auth")?.Value;
    if (string.IsNullOrWhiteSpace(authClaim) || authClaim != requirement.RequiredAuthValue) return;

    // Ensure token is younger or same age as LastToken
    if (user.LastTokenPurge == null) context.Succeed(requirement);

    string? iatString = context.User.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
    if (!long.TryParse(iatString, out long iatSeconds)) return;

    DateTime tokenTime = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;

    if (tokenTime >= user.LastTokenPurge) context.Succeed(requirement);
  }
}
