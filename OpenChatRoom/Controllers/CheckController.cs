using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("check")]
public class CheckController(IJWTBuilder JWTBuilder, UserAccessor accessor, AppDbContext dbContext) : ControllerBase
{
  private readonly IJWTBuilder _JWTBuilder = JWTBuilder;
  private readonly UserAccessor _accessor = accessor;
  private readonly AppDbContext _dbContext = dbContext;

  [HttpGet]
  public ActionResult answerCheck()
  {
    return Ok();
  }

  [HttpGet]
  [Route("auth")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult checkUser()
  {
    return Ok();
  }

  [HttpGet]
  [Route("refresh")]
  [Authorize(Policy = "AllowExpired")]
  public ActionResult<string> refreshToken()
  {
    User user = _accessor.GetCurrentUser(HttpContext)!;

    string jti = HttpContext.User.FindFirst(JwtRegisteredClaimNames.Jti)!.Value!;
    UserToken userToken = _dbContext.UserTokens.FirstOrDefault(t => t.Id == jti)!;

    _dbContext.UserTokens.Remove(userToken);

    return Ok(_JWTBuilder.generateToken(_dbContext, user.Id, true));
  }
}
