using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("friendRequest")]
[Authorize(Policy = "Authenticated")]
public class FriendRequestController : ControllerBase
{
  private readonly AppDbContext _context;
  public FriendRequestController(AppDbContext context) => _context = context;

  [HttpGet]
  [Produces("application/json")]
  public ActionResult<JsonArray> GetFriendRequests()
  {
    string userId = User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value;
    User? user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
    if (user == null) return Unauthorized("Your session is not saved");

    List<FriendRequest> friendRequests = [];
    friendRequests.AddRange(user.SentRequests);
    friendRequests.AddRange(user.ReceivedRequests);

    return Ok(friendRequests);
  }

  [HttpPost("send")]
  [Consumes("text/plain")]
  public ActionResult SendFriendRequest(string receiverId)
  {
    string userId = User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value;
    User? author = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
    if (author == null) return Unauthorized("Your session is not saved");

    User? receiver = _context.Users.Where(u => u.Id == receiverId).FirstOrDefault();
    if (receiver == null) return NotFound("Could not find such user");

    FriendRequest friendRequest = new(author, receiver);
    _context.FriendRequests.Add(friendRequest);
    _context.SaveChanges();

    return Ok();
  }

  [HttpGet("accept/{authorId}")]
  [Consumes("text/plain")]
  public ActionResult AcceptFriendRequest(string authorId)
  {
    string userId = User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value;
    User? user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
    if (user == null) return Unauthorized("Your session is not saved");

    FriendRequest? friendRequest = user.ReceivedRequests.Find(f => f.AuthorId == authorId);

    if (friendRequest == null) return NotFound("The request was not found");

    friendRequest.IsAccepted = true;
    _context.FriendRequests.Update(friendRequest);
    _context.SaveChanges();
    return Ok();
  }

  [HttpDelete("remove")]
  [Consumes("text/plain")]
  public ActionResult RemoveFriendRequest(string friendId)
  {
    string userId = User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value;
    User? user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
    if (user == null) return Unauthorized("Your session is not saved");

    FriendRequest? friendRequest = user.SentRequests.Find(f => f.ReceiverId == friendId);
    friendRequest ??= user.ReceivedRequests.Find(f => f.AuthorId == friendId);
    if (friendRequest == null) return NotFound("Could not find such request");

    _context.FriendRequests.Remove(friendRequest);
    _context.SaveChanges();

    return Ok();
  }
}
