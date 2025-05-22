using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;


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

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        return Ok(await userCollection.Find(_ => true).ToListAsync());
    }

    [HttpPost]
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
}
