using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

public class CustomAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
  private readonly AuthorizationMiddlewareResultHandler _default = new();

  public async Task HandleAsync(
      RequestDelegate next,
      HttpContext httpContext,
      AuthorizationPolicy policy,
      PolicyAuthorizationResult result)
  {
    // JwtValidityRequirement did not succeed
    if (policy.Requirements.Any(r => r is JwtValidityRequirement or TokenLifetimeRequirement) && (result.Challenged || result.Forbidden))
    {
      httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
      httpContext.Response.ContentType = "text/plain";

      string responseMsg = "Unauthorized";

      if (httpContext.Items.TryGetValue("ErrorMessage", out var msg) && msg is string msgString)
        responseMsg = string.Concat(responseMsg, ": ", msgString);

      await httpContext.Response.WriteAsync(responseMsg);
      return;
    }

    await _default.HandleAsync(next, httpContext, policy, result);
  }
}

