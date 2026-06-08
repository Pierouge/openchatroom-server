using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("channel")]
public class ChannelController(AppDbContext context, IConfiguration configuration, UserAccessor accessor) : ControllerBase
{
  private readonly AppDbContext _context = context;
  private readonly IConfigurationSection configurationSection = configuration.GetSection(
      "UserConfig"
  );
  private readonly UserAccessor _accessor = accessor;

  [HttpGet("{channelId}/{page}")]
  [Authorize(Policy = "Authenticated")]
  public ActionResult<JsonArray> GetChannelMessages(string channelId, int page)
  {
    if (string.IsNullOrEmpty(channelId))
      return BadRequest("Missing data");

    Channel? channel = _context.Channels.Where(c => c.Id == channelId).FirstOrDefault();
    if (channel == null)
      return NotFound("Such channel was not found");

    // Then check if the user has access to such channel
    User user = _accessor.GetCurrentUser(HttpContext)!;

    if (ChannelManager.findChannelMembers(channel).Find(u => u == user) == null)
      return Forbid("User does not have access to such channel");

    List<User> users = ChannelManager.findChannelMembers(channel);

    List<Message> messages = ChannelManager.getChannelMessages(
            _context,
            channel,
            limit: configurationSection.GetValue<int>("MessageCountPerRequest"),
            page: page
        );

    Dictionary<string, string> returnDict = new(){
      { "users", JsonSerializer.Serialize(users)},
      {"messages", JsonSerializer.Serialize(messages)}
    };

    // Finally return the messages and users
    return Ok(returnDict);
  }
}

