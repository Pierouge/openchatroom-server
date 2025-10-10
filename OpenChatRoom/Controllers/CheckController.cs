using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("check")]
public class CheckController : ControllerBase
{
    private readonly AppDbContext _context;
    public CheckController(AppDbContext context) => _context = context;

    [HttpGet]
    public IActionResult answerCheck()
    {
        return Ok();
    }

    [HttpGet]
    [Route("{username}")]
    public IActionResult checkUser(string username)
    {
        ISession session = HttpContext.Session;

        // Session-wise checking
        if (string.IsNullOrEmpty(username))
            return BadRequest("Expected a username");
        string? sessionUsername = session.GetString("username");
        _ = bool.TryParse(session.GetString("logged_in"), out bool isLoggedIn);
        if (isLoggedIn && username == sessionUsername) return Ok();

        // Refresh Session
        User? user = _context.Users.Where(u => u.Username == username).FirstOrDefault();
        if (user == null) return NotFound("No such user found");
        RefreshToken? refreshToken = _context.RefreshTokens.Where(r => r.User == user).FirstOrDefault();
        if (refreshToken == null) return Unauthorized();

        // Check if the token is correct
        string? tokenString = Request.Cookies["OpenChatRoom.Refresh"];
        if (string.IsNullOrEmpty(tokenString) || tokenString != refreshToken.Token)
            return Unauthorized();

        // Reopen the session
        HttpContext.Session.SetString("username", username);
        HttpContext.Session.SetString("logged_in", bool.TrueString);

        // Update the refresh token
        _context.RefreshTokens.Remove(refreshToken);
        RefreshToken newRefreshToken = new(user);
        _context.RefreshTokens.Add(newRefreshToken);

        Response.Cookies.Append(
            "OpenChatRoom.Refresh",
            newRefreshToken.Token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.Now.AddDays(15)
            }
        );

        _context.SaveChanges();
        
        return Ok();
    }
}