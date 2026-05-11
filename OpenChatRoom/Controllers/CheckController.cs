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
    User? user = _accessor.GetCurrentUser(HttpContext);
    if (user == null) return Unauthorized("Your session is not saved");

    string jwt = _JWTBuilder.generateToken(_dbContext, user.Id, true);

    return Ok(jwt);
  }
}
