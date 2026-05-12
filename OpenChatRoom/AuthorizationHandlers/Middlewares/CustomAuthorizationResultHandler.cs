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
    // Authorization did not succeed → normally 401
    if (result.Challenged || result.Forbidden)
    {
      int status = 401;

      if (result.Forbidden) status = 403;

      if (httpContext.Items.TryGetValue("ErrorCode", out var codeObj) &&
          codeObj is int customCode)
      {
        status = customCode;
      }

      httpContext.Response.StatusCode = status;
      httpContext.Response.ContentType = "text/plain";

      if (httpContext.Items.TryGetValue("ErrorMessage", out var msg) && msg is string msgString)
      {
        await httpContext.Response.WriteAsync(msgString);
        return;
      }

      if (result.Challenged) await httpContext.Response.WriteAsync("Unauthorized");
      else await httpContext.Response.WriteAsync("Forbidden");
      return;
    }

    await _default.HandleAsync(next, httpContext, policy, result);
  }
}

