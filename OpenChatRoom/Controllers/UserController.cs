using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SecureRemotePassword;

[ApiController]
[Route("user")]
public class UsersController : ControllerBase
{
    private readonly IMongoCollection<User> userCollection;

    // Set the correct collection
    public UsersController(IConfiguration iconf, IMongoClient mongoClient)
    {
        string? db = iconf.GetValue<string>("MongoDatabaseName");
        userCollection = mongoClient.GetDatabase(db).GetCollection<User>("user");
    }

    [HttpGet("{userName}")]
    public ActionResult<string> GetSalt(string userName)
    {
        User? user = userCollection.Find(u => u.userName == userName).FirstOrDefault();
        if (user != null) return Ok(user.salt);
        else return NotFound("Impossible to find such user.");
    }

    [HttpGet("{username}/{clientEphemeralPublic}")]
    public ActionResult GetSRPInfo(string userName, string clientEphemeralPublic)
    {
        // TODO: Phase 2 of SRP Handshake
        return Ok();
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult> Create(User user)
    {
        // Check type of input
        if (user == null) return BadRequest("Invalid request: Expected a JSON object.");

        // Check if user already exists
        User? existingUser = userCollection.Find(u => u.userName == user.userName).FirstOrDefault();
        if (existingUser != null) return Conflict("User already exists.");

        // Check if the data sent respects pre-set rules in DB
        try
        {
            await userCollection.InsertOneAsync(user);
            return Created();
        }
        catch (MongoCommandException)
        {
            return BadRequest("Database validation failed.");
        }
    }

    [HttpPost]
    [Consumes("text/plain")]
    public ActionResult<string> sendSRPM2(string clientSessionProof)
    {
        // TODO: Final server part of SRP Handshake
        return Ok();
    }
}
