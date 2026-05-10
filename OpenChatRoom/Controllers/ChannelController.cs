using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("channel")]
public class ChannelController(AppDbContext context, IConfiguration configuration) : ControllerBase
{
  private readonly AppDbContext _context = context;
  private readonly IConfigurationSection configurationSection = configuration.GetSection(
      "UserConfig"
  );

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
    string? userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
      return Unauthorized("Your session is not saved");
    User? user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
    if (user == null)
      return Unauthorized("The user was not found");

    if (ChannelManager.findChannelMembers(channel).Find(u => u == user) == null)
      return Forbid("User does not have access to such channel");

    // Finally return the messages
    return Ok(
        ChannelManager.getChannelMessages(
            _context,
            channel,
            limit: configurationSection.GetValue<int>("MessageCountPerRequest"),
            page: page
        )
    );
  }
}

