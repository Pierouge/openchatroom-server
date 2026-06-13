using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("friendRequest")]
[Authorize(Policy = AuthorizationType.Authenticated)]
public class FriendRequestController(AppDbContext context, UserAccessor accessor) : ControllerBase
{
  private readonly AppDbContext _context = context;
  private readonly UserAccessor _accessor = accessor;

  [HttpGet]
  [Produces("application/json")]
  public ActionResult<JsonArray> GetFriendRequests()
  {
    User? user = _accessor.GetCurrentUser(HttpContext)!;

    List<FriendRequest> friendRequests = [];
    friendRequests.AddRange(user.SentRequests);
    friendRequests.AddRange(user.ReceivedRequests);

    return Ok(friendRequests);
  }

  [HttpPost("send")]
  [Consumes("text/plain")]
  public ActionResult SendFriendRequest(string receiverId)
  {
    User? author = _accessor.GetCurrentUser(HttpContext)!;

    User? receiver = _context.Users.Where(u => u.Id == receiverId).FirstOrDefault();
    if (receiver == null) return NotFound("Could not find such user");

    FriendRequest friendRequest = new(author, receiver);
    _context.FriendRequests.Add(friendRequest);
    _context.SaveChanges();

    return Ok();
  }

  [HttpPatch("{authorId}")]
  [Consumes("text/plain")]
  public ActionResult AcceptFriendRequest([FromRoute] string authorId)
  {
    User? user = _accessor.GetCurrentUser(HttpContext)!;

    FriendRequest? friendRequest = user.ReceivedRequests.Find(f => f.AuthorId == authorId);

    if (friendRequest == null) return NotFound("The request was not found");

    friendRequest.IsAccepted = true;
    _context.FriendRequests.Update(friendRequest);
    _context.SaveChanges();
    return Ok();
  }

  [HttpDelete("{friendId}")]
  [Consumes("text/plain")]
  public ActionResult RemoveFriendRequest([FromRoute] string friendId)
  {
    User? user = _accessor.GetCurrentUser(HttpContext)!;

    FriendRequest? friendRequest = user.SentRequests.Find(f => f.ReceiverId == friendId);
    friendRequest ??= user.ReceivedRequests.Find(f => f.AuthorId == friendId);
    if (friendRequest == null) return NotFound("Could not find such request");

    _context.FriendRequests.Remove(friendRequest);
    _context.SaveChanges();

    return Ok();
  }
}
