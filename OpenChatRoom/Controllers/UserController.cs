using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureRemotePassword;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security;
using Microsoft.AspNetCore.Http.HttpResults;

[ApiController]
[Route("user")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsersController(AppDbContext context) => _context = context;

    [HttpPost("create")]
    [Consumes("application/json")]
    [ValidateAntiForgeryToken]
    public ActionResult Create(JsonArray jsonArray)
    {
        if (jsonArray == null) return BadRequest("Invalid request: Expected a JSON array.");
        if (jsonArray.Count != 2) return BadRequest("Invalid request: Wrong size of the JsonArray");
        string userString = jsonArray[0]!.ToJsonString();

        User? user = JsonSerializer.Deserialize<User>(userString);
        bool saveLogin = jsonArray[1]!.GetValue<bool>();

        // Check type of input
        if (user == null) return BadRequest("Invalid request: Expected userdata first.");

        user.IsAdmin = false; // Change to be sure, to counter the funny hacked client

        // Check if user already exists
        User? existingUser = _context.Users.Where(u => u.Username == user.Username).FirstOrDefault();
        if (existingUser != null) return Conflict("Username already exists.");

        // Check if the data sent respects pre-set rules in DB
        try
        {
            _context.Users.Add(user);
            if (saveLogin)
            {
                RefreshToken refreshToken = new(user);
                Response.Cookies.Append(
                    "OpenChatRoom.Refresh",
                    refreshToken.Token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Expires = DateTime.Now.AddDays(15)
                    }
                );
                _context.RefreshTokens.Add(refreshToken);
            }
            _context.SaveChanges();

            // Add the username to the session
            HttpContext.Session.SetString("username", user.Username);
            // Add a login flag for the session
            HttpContext.Session.SetString("logged_in", bool.TrueString);

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

        string salt = user.Salt;
        string verifier = user.Verifier;

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
    [Consumes("application/json")]
    [Produces("text/plain")]
    [IgnoreAntiforgeryToken]
    public ActionResult<string> sendSRPM2(JsonObject jsonObject) // Phase 4 of SRP
    {
        Dictionary<string, string>? requestValues = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonObject);
        if (requestValues == null) return BadRequest("Error: expected a jsonObject");
        string clientSessionProof = requestValues["proof"];
        _ = bool.TryParse(requestValues["saveLogin"], out bool saveLogin);

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
        SrpSession serverSession;
        try
        {
            serverSession = server.DeriveSession(serverEphemeralSecret, clientPublicEphemeral, salt, username,
                verifier, clientSessionProof);
        }
        catch (SecurityException)
        {
            return Unauthorized("Wrong Credentials");
        }

        // Clean the Data
        HttpContext.Session.Remove("server_secret_ephemeral");
        HttpContext.Session.Remove("client_public_ephemeral");
        HttpContext.Session.Remove("verifier");
        HttpContext.Session.Remove("salt");

        // Add a login flag for the session
        HttpContext.Session.SetString("logged_in", bool.TrueString);

        if (saveLogin)
        {
            User? user = _context.Users.Where(u => u.Username == username).FirstOrDefault();
            if (user!.RefreshToken != null) _context.RefreshTokens.Remove(user.RefreshToken);
            RefreshToken refreshToken = new(user!);
            Response.Cookies.Append(
                "OpenChatRoom.Refresh",
                refreshToken.Token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.Now.AddDays(15)
                }
            );
            _context.RefreshTokens.Add(refreshToken);
            _context.SaveChanges();
        }

        return Ok(serverSession.Proof);
    }

    [IgnoreAntiforgeryToken]
    [HttpGet("userInfo")]
    [Produces("application/json")]
    public ActionResult GetUserInfo(string username)
    {
        bool.TryParse(HttpContext.Session.GetString("logged_in"), out bool loggedIn);
        if (!loggedIn)
            return Unauthorized("Your are not logged in.");
        if (string.IsNullOrEmpty(username))
            return BadRequest("Missing a username");
        User? user = _context.Users.Where(u => u.Username == username).FirstOrDefault();
        if (user == null) return NotFound("Such user does not exist");
        Dictionary<string, string> returnDict = new()
        {
            {"Id", user.Id},
            {"Username", user.Username},
            {"VisibleName", user.VisibleName},
            {"IsAdmin", user.IsAdmin.ToString()}
        };
        return Ok(returnDict);
    }

    [IgnoreAntiforgeryToken]
    [HttpGet("friendRequests")]
    [Produces("application/json")]
    public ActionResult GetFriendRequests()
    {
        _ = bool.TryParse(HttpContext.Session.GetString("logged_in"), out bool isLoggedIn);
        if (!isLoggedIn) return Unauthorized("Your session is not saved");
        
        string? sessionUsername = HttpContext.Session.GetString("username");
        if (sessionUsername == null) return Unauthorized("Your session is not saved");
        User? user = _context.Users.Where(u => u.Username == sessionUsername).FirstOrDefault();
        if (user == null) return Unauthorized("Your session is not saved");

        List<FriendRequest> friendRequests = _context.FriendRequests.Where(f => f.Author == user || f.Receiver == user).ToList();

        return Ok(friendRequests);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("sendFriendRequest")]
    [Consumes("text/plain")]
    public ActionResult SendFriendRequest(string receiverId)
    {
        _ = bool.TryParse(HttpContext.Session.GetString("logged_in"), out bool isLoggedIn);
        if (!isLoggedIn) return Unauthorized("Your session is not saved");
        string? sessionUsername = HttpContext.Session.GetString("username");
        if (sessionUsername == null) return Unauthorized("Your session is not saved");
        User? author = _context.Users.Where(u => u.Username == sessionUsername).FirstOrDefault();
        if (author == null) return Unauthorized("Your session is not saved");

        User? receiver = _context.Users.Where(u => u.Id == receiverId).FirstOrDefault();
        if (receiver == null) return NotFound("Could not find such user");

        FriendRequest friendRequest = new(author, receiver);
        _context.FriendRequests.Add(friendRequest);
        _context.SaveChanges();

        return Accepted();
    }
    
    [ValidateAntiForgeryToken]
    [HttpGet("acceptFriendRequest")]
    [Consumes("text/plain")]
    public ActionResult AcceptFriendRequest(string authorId)
    {
        _ = bool.TryParse(HttpContext.Session.GetString("logged_in"), out bool isLoggedIn);
        if (!isLoggedIn) return Unauthorized("Your session is not saved");
        string? sessionUsername = HttpContext.Session.GetString("username");
        if (sessionUsername == null) return Unauthorized("Your session is not saved");
        User? user = _context.Users.Where(u => u.Username == sessionUsername).FirstOrDefault();
        if (user == null) return Unauthorized("Your session is not saved");

        FriendRequest? friendRequest = _context.FriendRequests.Where(f => f.AuthorId == authorId
            && f.Receiver == user).FirstOrDefault();

        if (friendRequest == null) return NotFound("The request was not found");

        friendRequest.IsAccepted = true;
        _context.FriendRequests.Update(friendRequest);
        _context.SaveChanges();
        return Ok();
    }

    [IgnoreAntiforgeryToken]
    [HttpGet("terminateSession")]
    public ActionResult TerminateSession()
    {
        string? sessionUsername = HttpContext.Session.GetString("username");
        if (!string.IsNullOrEmpty(sessionUsername))
        {
            User? user = _context.Users.Where(u => u.Username == sessionUsername).FirstOrDefault();
            if (user != null && user.RefreshToken != null)
            {
                _context.RefreshTokens.Remove(user.RefreshToken);
                _context.SaveChanges();
            }
        }
        Response.Cookies.Delete("OpenChatRoom.Refresh");
        HttpContext.Session.Clear();
        return Ok();
    }

    [ValidateAntiForgeryToken]
    [HttpPost("edit")]
    public ActionResult EditProfile(User user)
    {
        _ = bool.TryParse(HttpContext.Session.GetString("logged_in"), out bool isLoggedIn);
        if (!isLoggedIn) return Unauthorized("Your session is not saved");
        string? sessionUsername = HttpContext.Session.GetString("username");
        if (sessionUsername == null) return Unauthorized("Your session is not saved");
        User? sessionUser = _context.Users.Where(u => u.Username == sessionUsername).FirstOrDefault();
        if (sessionUser == null) return Unauthorized("Your session is not saved");
        try
        {
            sessionUser.Username = user.Username;
            sessionUser.VisibleName = user.VisibleName;
            sessionUser.Verifier = user.Verifier;
            sessionUser.Salt = user.Salt;
            _context.Users.Update(sessionUser);
            _context.SaveChanges();

            return Ok();
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

    [ValidateAntiForgeryToken]
    [HttpDelete("delete")]
    public ActionResult RemoveUser(bool removeMessages)
    {
        _ = bool.TryParse(HttpContext.Session.GetString("logged_in"), out bool isLoggedIn);
        if (!isLoggedIn) return Unauthorized("Your session is not saved");
        string? sessionUsername = HttpContext.Session.GetString("username");
        if (sessionUsername == null) return NotFound("User not found");
        User? user = _context.Users.Where(u => u.Username == sessionUsername).FirstOrDefault();
        if (user == null) return NotFound("User not found");
        _context.Users.Remove(user);
        if (removeMessages)
        {
            List<Message> messages = _context.Messages.Where(m => m.Author == user).ToList();
            _context.Remove(messages);
        }
        _context.SaveChanges();
        TerminateSession();
        return Accepted();
    }
}
