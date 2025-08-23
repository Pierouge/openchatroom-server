using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureRemotePassword;

[ApiController]
[Route("user")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsersController(AppDbContext context) => _context = context;

    [HttpGet("getSalt/{userName}")]
    public ActionResult<string> GetSalt(string userName)
    {
        User? user =  _context.Users.Where(u => u.Username == userName).FirstOrDefault();
        if (user != null) return Ok(user.Salt);
        else return NotFound("Impossible to find such user.");
    }

    [HttpGet("getSRPInfo/{username}/{clientEphemeralPublic}")]
    public ActionResult GetSRPInfo(string userName, string clientEphemeralPublic)
    {
        // TODO: Phase 2 of SRP Handshake
        return Ok();
    }

    [HttpPost("create")]
    [Consumes("application/json")]
    public async Task<ActionResult> Create(User user)
    {
        // Check type of input
        if (user == null) return BadRequest("Invalid request: Expected a JSON object.");

        // Check if user already exists
        User? existingUser = _context.Users.Where(u => u.Username == user.Username).FirstOrDefault();
        if (existingUser != null) return Conflict("User already exists.");

        // Check if the data sent respects pre-set rules in DB
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Created();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is MySqlConnector.MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062) // Error 1062 is the DUPLICATE exception error in MySQL
                {
                    return Conflict("This user already exists");
                }
                return BadRequest($"SQL Error {mySqlEx.Number}: {mySqlEx.Message}");
            }
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("srp-m2")]
    [Consumes("text/plain")]
    public async Task<ActionResult<string>> sendSRPM2()
    {
        using var reader = new StreamReader(Request.Body);
        string clientSessionProof = await reader.ReadToEndAsync();
        // TODO: Final server part of SRP Handshake
        return Ok(clientSessionProof);
    }
}
