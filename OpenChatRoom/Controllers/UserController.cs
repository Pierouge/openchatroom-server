using System.IdentityModel.Tokens.Jwt;
using System.Security;
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
  [AllowAnonymous]
  public ActionResult<UserControllerRecords.CreateResult> Create([FromBody] UserControllerRecords.CreateUserRequest requestBody)
  {
    User user = new(requestBody.Username, requestBody.VisibleName, requestBody.Salt, requestBody.Verifier);

    // Check if user already exists
    User? existingUser = _dbContext
        .Users.Where(u => u.Username == user.Username)
        .FirstOrDefault();
    if (existingUser != null)
      return Conflict("Username already exists.");

    // Check if the data sent respects pre-set rules in DB
    try
    {
      _dbContext.Users.Add(user);
      _dbContext.SaveChanges();

      (string jwt, string? refreshJwt) = _JWTBuilder.GenerateToken(_dbContext, user.Id, true);

      return Created($"info/{user.Username}", new UserControllerRecords.CreateResult(
        user.GetInfo(),
        new TokenPair(jwt, refreshJwt!)
      ));
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

  [HttpPost("srp/1")]
  [AllowAnonymous]
  public ActionResult<UserControllerRecords.SrpStep2Response> GetSRPInfo([FromBody] UserControllerRecords.SrpStep1Request requestBody) // Phase 2 of SRP Handshake
  {

    //First get info about the current user (check if he exists)
    User? user = _dbContext.Users.Where(u => u.Username == requestBody.Username).FirstOrDefault();
    if (user == null)
      return NotFound("Impossible to find such user.");

    string salt = user.Salt;
    string verifier = user.Verifier;

    // Generates the Ephemeral
    SrpEphemeral serverEphemeral = new SrpServer().GenerateEphemeral(verifier);

    // Stores the server private Ephemeral and the client public ephemeral in the loginStorage 

    LoginStorage.Data localData = new(
        serverEphemeral.Secret,
        requestBody.ClientEphemeralPublic,
        salt,
        verifier
    );

    _loginStorage.addEntry(user.Id, localData);

    // Partial Token to ensure the safety of the protocol
    string jwt = _JWTBuilder.GenerateToken(_dbContext, user.Id, false).Token;

    return Ok(new UserControllerRecords.SrpStep2Response(salt, serverEphemeral.Public, jwt));
  }

  [HttpPost("srp/2")]
  [Authorize]
  public ActionResult<UserControllerRecords.SrpStep4Response> SendSRPM2([FromBody] string clientSessionProof) // Phase 4 of SRP
  {
    if (string.IsNullOrWhiteSpace(clientSessionProof))
      return BadRequest("Error: expected a client session proof");

    User? user = _accessor.GetCurrentUser(HttpContext);
    if (user == null) return Unauthorized("Your session is not saved");

    LoginStorage.Data? loginData = _loginStorage.getEntry(user.Id);
    if (loginData == null) return Unauthorized("The session has not been saved.");

    SrpServer server = new();
    SrpSession serverSession;
    try
    {
      serverSession = server.DeriveSession(
          loginData.ServerSecretEphemeral,
          loginData.ClientPublicEphemeral,
          loginData.Salt,
          user.Username,
          loginData.Verifier,
          clientSessionProof
      );
    }
    catch (SecurityException)
    {
      return Unauthorized("Wrong Credentials");
    }

    // Clean the Data
    _loginStorage.removeEntry(user.Id);

    return Ok(new UserControllerRecords.SrpStep4Response(serverSession.Proof,
          TokenPair.FromJWTServiceResult(_JWTBuilder.GenerateToken(_dbContext, user.Id, true))));
  }

  [HttpGet("info/{username}")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult<User.UserInfo> GetUserInfo([FromRoute] string username)
  {

    if (string.IsNullOrEmpty(username))
      return BadRequest("Missing a userid");

    User? user = _dbContext.Users.Where(u => u.Username == username).FirstOrDefault();

    if (user == null)
      return NotFound("Such user does not exist");

    return Ok(user.GetInfo());
  }

  [HttpGet("privateChannels/{page}")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult<List<Channel>> GetPrivateChannels([FromRoute] int page)
  {
    User user = _accessor.GetCurrentUser(HttpContext)!;

    int pageSize = configurationSection.GetValue<int>("ChannelCountPerRequest");

    List<Channel> channels =
    [
        .. _dbContext
                .Channels.Where(c => c.Members.Any(m => m.Id == user.Id))
                .OrderByDescending(c => c.LastMessage)
                .Skip((page - 1) * pageSize)
                .Take(pageSize),
        ];

    return Ok(channels);
  }

  [HttpGet("servers")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult<List<Server>> GetChannels()
  {
    User user = _accessor.GetCurrentUser(HttpContext)!;

    List<Server> servers = [
      .. _dbContext.Servers.Where(s => s.Members.Any(m => m.Id == user.Id))
    ];

    return Ok(servers);
  }

  [HttpPost("edit")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult EditProfile([FromBody] UserControllerRecords.CreateUserRequest userRequest)
  {
    User sessionUser = _accessor.GetCurrentUser(HttpContext)!;

    try
    {
      sessionUser.Username = userRequest.Username;
      sessionUser.VisibleName = userRequest.VisibleName;
      sessionUser.Verifier = userRequest.Verifier;
      sessionUser.Salt = userRequest.Salt;
      _dbContext.Users.Update(sessionUser);
      _dbContext.SaveChanges();

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

  [HttpDelete("terminateSession")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult TerminateSession()
  {
    string jti = HttpContext.User.FindFirst(JwtRegisteredClaimNames.Jti)!.Value!;
    UserToken userToken = _dbContext.UserTokens.FirstOrDefault(t => t.Id == jti)!;

    _dbContext.UserTokens.Remove(userToken);
    _dbContext.SaveChanges();

    return Ok();
  }

  [HttpDelete("nukeSessions")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult NukeSessions()
  {
    User user = _accessor.GetCurrentUser(HttpContext)!;

    List<UserToken> userTokens = [.. _dbContext.UserTokens.Where(t => t.UserId == user.Id)];

    _dbContext.UserTokens.RemoveRange(userTokens);
    _dbContext.SaveChanges();

    return Ok();
  }

  [HttpDelete()]
  [Authorize(Policy = "Authenticated")]
  public ActionResult RemoveUser(bool removeMessages)
  {
    User user = _accessor.GetCurrentUser(HttpContext)!;

    _dbContext.Users.Remove(user);
    if (removeMessages)
    {
      List<Message> messages = [.. _dbContext.Messages.Where(m => m.Author == user)];
      _dbContext.RemoveRange(messages);
    }
    _dbContext.SaveChanges();
    return Ok();
  }
}
