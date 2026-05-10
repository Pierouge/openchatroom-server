using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("check")]
public class CheckController(IJWTBuilder JWTBuilder, UserAccessor accessor) : ControllerBase
{
  private readonly IJWTBuilder _JWTBuilder = JWTBuilder;
  private readonly UserAccessor _accessor = accessor;

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
    User? user = _accessor.GetCurrentUser();
    if (user == null) return Unauthorized("Your session is not saved");

    string jwt = _JWTBuilder.generateToken(user.Id, true);

    return Ok(jwt);
  }
}
