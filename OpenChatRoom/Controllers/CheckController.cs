using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("check")]
public class CheckController(IJWTBuilder JWTBuilder, AppDbContext context) : ControllerBase
{
  private readonly IJWTBuilder _JWTBuilder = JWTBuilder;
  private readonly AppDbContext _context = context;

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
    string? userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
      return Unauthorized("Your session is not saved");
    User? user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
    if (user == null) return Unauthorized("Your session is not saved");

    string jwt = _JWTBuilder.generateToken(userId, true);

    return Ok(jwt);
  }
}
