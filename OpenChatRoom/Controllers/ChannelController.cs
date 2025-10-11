using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("channel")]
public class ChannelController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfigurationSection configurationSection;
    public ChannelController(AppDbContext context, IConfiguration configuration) 
    {
        _context = context;
        configurationSection = configuration.GetSection("UserConfig");
    }

    [HttpGet("{channelId}/{page}")]
    public ActionResult<JsonArray> GetChannelMessages(string channelId, int page)
    {

        if (string.IsNullOrEmpty(channelId)) return BadRequest("Missing data");

        // First fetch all data
        User? user = SessionChecker.fetchUserBySession(HttpContext.Session, _context);
        if (user == null) return Unauthorized("Your session is not saved");

        Channel? channel = _context.Channels.Where(c => c.Id == channelId).FirstOrDefault();
        if (channel == null) return NotFound("Such channel was not found");

        // Then check if the user has access to such channel
        if (ChannelManager.findChannelMembers(channel).Find(u => u == user) == null)
            return Forbid("User does not have access to such channel");

        // Finally return the messages
        return Ok(ChannelManager.getChannelMessages(_context, channel,
            limit: configurationSection.GetValue<int>("MessageCountPerRequest"),
            page:page));

    }

}