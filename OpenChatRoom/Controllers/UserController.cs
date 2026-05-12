using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureRemotePassword;

[ApiController]
[Route("user")]
public partial class UserController(AppDbContext context, IConfiguration configuration, IJWTBuilder JWTBuilder, ILoginStorage loginStorage, UserAccessor accessor)
    : ControllerBase
{
  private readonly AppDbContext _dbContext = context;
  private readonly IConfigurationSection configurationSection = configuration.GetSection(
      "UserConfig"
  );
  private readonly IJWTBuilder _JWTBuilder = JWTBuilder;
  private readonly ILoginStorage _loginStorage = loginStorage;
  private readonly UserAccessor _accessor = accessor;

  [HttpPost("create")]
  [Consumes("application/json")]
  [AllowAnonymous]
  public ActionResult Create(JsonArray jsonArray)
  {
    if (jsonArray == null)
      return BadRequest("Invalid request: Expected a JSON array.");
    if (jsonArray.Count != 2)
      return BadRequest("Invalid request: Wrong size of the JsonArray");
    string userString = jsonArray[0]!.ToJsonString();

    User? user = JsonSerializer.Deserialize<User>(userString);

    // Check type of input
    if (user == null)
      return BadRequest("Invalid request: Expected userdata first.");

    user.IsAdmin = false; // Change to be sure, to counter the funny hacked client
    user.Id = IdGenerator.generateId(); // Change to make sure that the user has an id

    // Check if user already exists
    User? existingUser = _dbContext
        .Users.Where(u => u.Username == user.Username)
        .FirstOrDefault();
    if (existingUser != null)
      return Conflict("Username already exists.");

    // Check if the username is valid
    if (!UsernameRegex().IsMatch(user.Username))
      return BadRequest("Invalid request: Username possess illegal characters");

    // Check if the data sent respects pre-set rules in DB
    try
    {
      _dbContext.Users.Add(user);
      _dbContext.SaveChanges();

      string jwt = _JWTBuilder.generateToken(_dbContext, user.Id, true);

      return Created($"info/{user.Username}", new
      {
        user.Id,
        user.Username,
        user.VisibleName,
        user.IsAdmin,
        token = jwt
      });
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
    catch (InvalidOperationException)
    {
      return BadRequest("Non-nullable fields of the entity are missing");
    }
  }

  [HttpGet("getSRPInfo/{username}/{clientEphemeralPublic}")]
  [Produces("application/json")]
  [AllowAnonymous]
  public ActionResult<JsonObject> GetSRPInfo(string userName, string clientEphemeralPublic) // Phase 2 of SRP Handshake
  {
    //First get info about the current user (check if he exists)
    User? user = _dbContext.Users.Where(u => u.Username == userName).FirstOrDefault();
    if (user == null)
      return NotFound("Impossible to find such user.");

    string salt = user.Salt;
    string verifier = user.Verifier;

    // Generates the Ephemeral
    SrpEphemeral serverEphemeral = new SrpServer().GenerateEphemeral(verifier);

    // Stores the server private Ephemeral and the client public ephemeral in the loginStorage 
    Dictionary<string, string> localData = new()
    {
        {"server_secret_ephemeral", serverEphemeral.Secret},
        {"client_public_ephemeral", clientEphemeralPublic},
        {"username", userName},
        {"salt", salt},
        {"verifier", verifier}
    };

    if (!_loginStorage.addEntry(user.Id, localData))
      return Problem("Unable to add login data to secure session");

    // Generates the Response JSON
    Dictionary<string, string> returnDict = [];
    returnDict.Add("salt", salt);
    returnDict.Add("server_public_ephemeral", serverEphemeral.Public);

    // Partial Token to ensure the safety of the protocol
    string jwt = _JWTBuilder.generateToken(_dbContext, user.Id, false);
    returnDict.Add("token", jwt);

    return Ok(returnDict);
  }

  [HttpPost("srp-m2")]
  [Consumes("application/json")]
  [Produces("text/plain")]
  [Authorize]
  public ActionResult<string> SendSRPM2(JsonObject jsonObject) // Phase 4 of SRP
  {
    Dictionary<string, string>? requestValues = JsonSerializer.Deserialize<
        Dictionary<string, string>
    >(jsonObject);
    if (requestValues == null)
      return BadRequest("Error: expected a jsonObject");
    string clientSessionProof = requestValues["proof"];

    User? user = _accessor.GetCurrentUser(HttpContext);
    if (user == null) return Unauthorized("Your session is not saved");

    Dictionary<string, string>? loginData = _loginStorage.getEntry(user.Id);
    if (loginData == null) return Unauthorized("The session has not been saved.");

    // Get back data from phase 2
    string serverEphemeralSecret = loginData["server_secret_ephemeral"];
    string clientPublicEphemeral = loginData["client_public_ephemeral"];
    string verifier = loginData["verifier"];
    string salt = loginData["salt"];

    // Derive Server Session
    if (
        serverEphemeralSecret == null
        || clientPublicEphemeral == null
        || verifier == null
        || salt == null
    )
      return Unauthorized("The session has not been saved.");

    SrpServer server = new();
    SrpSession serverSession;
    try
    {
      serverSession = server.DeriveSession(
          serverEphemeralSecret,
          clientPublicEphemeral,
          salt,
          user.Username,
          verifier,
          clientSessionProof
      );
    }
    catch (SecurityException)
    {
      return Unauthorized("Wrong Credentials");
    }

    // Clean the Data
    _loginStorage.removeEntry(user.Id);

    Dictionary<string, string> returnDict = new(){
      {"proof", serverSession.Proof},
      {"token", _JWTBuilder.generateToken(_dbContext, user.Id, true)}
    };

    return Ok(returnDict);
  }

  [HttpGet("info/{username}")]
  [Produces("application/json")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult<JsonObject> GetUserInfo(string username)
  {
    if (string.IsNullOrEmpty(username))
      return BadRequest("Missing an id");
    User? user = _dbContext.Users.Where(u => u.Username == username).FirstOrDefault();
    if (user == null)
      return NotFound("Such user does not exist");
    Dictionary<string, string> returnDict = new()
        {
            { "Id", user.Id },
            { "Username", user.Username },
            { "VisibleName", user.VisibleName },
            { "IsAdmin", user.IsAdmin.ToString() },
        };
    return Ok(returnDict);
  }

  [HttpGet("privateChannels/{page}")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult<JsonArray> GetPrivateChannels(int page)
  {
    User? user = _accessor.GetCurrentUser(HttpContext);
    if (user == null) return Unauthorized("Your session is not saved");

    JsonArray returnArr = [];

    int pageSize = configurationSection.GetValue<int>("ChannelCountPerRequest");

    List<Channel> channels =
    [
        .. _dbContext
                .Channels.Where(c => c.PrivateChannelMembers.Any(m => m.Id == user.Id))
                .OrderByDescending(c => c.LastMessage)
                .Skip((page - 1) * pageSize)
                .Take(pageSize),
        ];

    foreach (Channel channel in channels)
    {
      Dictionary<string, string> dict = new()
            {
                { "Id", channel.Id },
                { "Name", channel.Name },
                { "LastMessage", channel.LastMessage.ToString() },
            };
      returnArr.Add(dict);
    }

    return Ok(returnArr);
  }

  [HttpPost("edit")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult EditProfile(User user)
  {
    User? sessionUser = _accessor.GetCurrentUser(HttpContext);
    if (sessionUser == null)
      return Unauthorized("Your session is not saved");

    if (!UsernameRegex().IsMatch(user.Username))
    {
      return BadRequest("Invalid request: Username possess illegal characters");
    }

    try
    {
      sessionUser.Username = user.Username;
      sessionUser.VisibleName = user.VisibleName;
      sessionUser.Verifier = user.Verifier;
      sessionUser.Salt = user.Salt;
      _dbContext.Users.Update(sessionUser);
      _dbContext.SaveChanges();

      string jwt = _JWTBuilder.generateToken(_dbContext, user.Id, true);

      return Ok(jwt);
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

  [HttpGet("terminateSession")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult TerminateSession()
  {
    User? user = _accessor.GetCurrentUser(HttpContext);
    if (user == null)
      return Unauthorized("Your session is not saved");

    string jti = HttpContext.User.FindFirst(JwtRegisteredClaimNames.Jti)!.Value!;
    UserToken userToken = _dbContext.UserTokens.FirstOrDefault(t => t.Id == jti)!;

    _dbContext.UserTokens.Remove(userToken);
    _dbContext.SaveChanges();

    return Ok();
  }

  [HttpGet("nukeSessions")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult NukeSessions()
  {
    User user = _accessor.GetCurrentUser(HttpContext)!;

    List<UserToken> userTokens = [.. _dbContext.UserTokens.Where(t => t.UserId == user.Id)];

    _dbContext.UserTokens.RemoveRange(userTokens);
    _dbContext.SaveChanges();

    return Ok();
  }

  [HttpDelete("delete")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult RemoveUser(bool removeMessages)
  {
    User? user = _accessor.GetCurrentUser(HttpContext)!;

    _dbContext.Users.Remove(user);
    if (removeMessages)
    {
      List<Message> messages = [.. _dbContext.Messages.Where(m => m.Author == user)];
      _dbContext.RemoveRange(messages);
    }
    _dbContext.SaveChanges();
    return Ok();
  }

  [GeneratedRegex("^[a-z0-9]+$", RegexOptions.Compiled)]
  private static partial Regex UsernameRegex();
}
