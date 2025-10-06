using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureRemotePassword;
using System.Security.Cryptography;

[ApiController]
[Route("user")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsersController(AppDbContext context) => _context = context;

    [HttpGet("terminateSession")]
    public ActionResult TerminateSession()
    {
        HttpContext.Session.Clear();
        return Ok();
    }

    [HttpPost("create")]
    [Consumes("application/json")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(User user)
    {
        // Check type of input
        if (user == null) return BadRequest("Invalid request: Expected a JSON object.");

        user.IsAdmin = false; // Change to be sure, to counter the funny hacked client

        // Check if user already exists
        User? existingUser = _context.Users.Where(u => u.Username == user.Username).FirstOrDefault();
        if (existingUser != null) return Conflict("Username already exists.");

        // Check if the data sent respects pre-set rules in DB
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Add the username to the session
            HttpContext.Session.SetString("username", user.Username);
            // Add a login flag for the session
            HttpContext.Session.SetString("logged_in", "true");

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

    [HttpGet("getSRPInfo/{username}/{clientEphemeralPublic}")]
    [Produces("application/json")]
    public ActionResult GetSRPInfo(string userName, string clientEphemeralPublic) // Phase 2 of SRP Handshake
    {
        //First get info about the current user (check if he exists)
        User? user = _context.Users.Where(u => u.Username == userName).FirstOrDefault();
        if (user == null) return NotFound("Impossible to find such user.");

        // To check if there's an old connection
        string? oldSessionId = Request.Cookies["OpenChatRoom.Session"];

        string salt = user.Salt;
        string verifier = user.Verifier;

        byte[]? key;
        if (!string.IsNullOrEmpty(oldSessionId)) key = HKDF.DeriveKey(hashAlgorithmName: HashAlgorithmName.SHA256,
                    ikm: Encoding.UTF8.GetBytes(oldSessionId),
                    salt: Encoding.UTF8.GetBytes(salt),
                    info: Encoding.UTF8.GetBytes(userName + "|" + verifier),
                    outputLength:32);

        // Generates the Ephemeral
        SrpEphemeral serverEphemeral = new SrpServer().GenerateEphemeral(verifier);

        // Stores the server private Ephemeral and the client public ephemeral for the session
        HttpContext.Session.SetString("server_secret_ephemeral", serverEphemeral.Secret);
        HttpContext.Session.SetString("client_public_ephemeral", clientEphemeralPublic);
        HttpContext.Session.SetString("username", userName);
        HttpContext.Session.SetString("salt", salt);
        HttpContext.Session.SetString("verifier", verifier);

        // Generates the Response JSON
        Dictionary<string, string> returnDict = [];
        returnDict.Add("salt", salt);
        returnDict.Add("server_public_ephemeral", serverEphemeral.Public);
        
        return Ok(returnDict);
    }

    [HttpPost("srp-m2")]
    [Consumes("text/plain")]
    [Produces("text/plain")]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<string>> sendSRPM2() // Phase 4 of SRP
    {
        // Gets the proof by reading the body
        using var reader = new StreamReader(Request.Body);
        string clientSessionProof = await reader.ReadToEndAsync();

        // Get back data from phase 2
        string? serverEphemeralSecret = HttpContext.Session.GetString("server_secret_ephemeral");
        string? clientPublicEphemeral = HttpContext.Session.GetString("client_public_ephemeral");
        string? username = HttpContext.Session.GetString("username");
        string? verifier = HttpContext.Session.GetString("verifier");
        string? salt = HttpContext.Session.GetString("salt");

        // Derive Server Session
        if (serverEphemeralSecret == null || clientPublicEphemeral == null
        || username == null || verifier == null || salt == null) return Unauthorized("The session has not been saved.");
        SrpServer server = new();
        SrpSession serverSession = server.DeriveSession(serverEphemeralSecret, clientPublicEphemeral, salt, username,
        verifier, clientSessionProof);

        // Clean the Data
        HttpContext.Session.Remove("server_secret_ephemeral");
        HttpContext.Session.Remove("client_public_ephemeral");
        HttpContext.Session.Remove("verifier");
        HttpContext.Session.Remove("salt");

        // Add a login flag for the session
        HttpContext.Session.SetString("logged_in", "true");

        return Ok(serverSession.Proof);
    }
}
