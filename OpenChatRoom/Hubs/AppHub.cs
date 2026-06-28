using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[Authorize(AuthorizationType.Authenticated)]
public class AppHub(UserAccessor userAccessor, AppDbContext dbContext) : Hub
{
  private readonly UserAccessor _accessor = userAccessor;
  private readonly AppDbContext _dbContext = dbContext;
  private readonly ConcurrentDictionary<string, string> connectionUser = new();

  private User? GetCurrentUserOrAbort()
  {
    var http = Context.GetHttpContext();
    if (http == null)
    {
      Context.Abort();
      return null;
    }

    // NOTE: User CAN be null if it was removed from the server earlier
    User? user = _accessor.GetCurrentUser(http);
    if (user == null)
      Context.Abort();

    return user;
  }

  public override async Task OnConnectedAsync()
  {
    User? user = GetCurrentUserOrAbort();
    if (user == null) return;

    // Link userId to ConnectionId
    connectionUser.AddOrUpdate(Context.ConnectionId, user.Id, (key, oldValue) => user.Id);

    // Add user to user group
    await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{user.Id}");

    // Now add to server groups
    List<Server> serverList = [.. _dbContext.Servers.Where(s => s.Members.Any(m => m.Id == user.Id))];
    foreach (Server server in serverList)
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, $"server:{server.Id}");
    }

    // And now add to channel list
    List<Channel> channelList = [.. _dbContext.Channels.Where(c => c.Members.Any(m => m.Id == user.Id))];
    foreach (Channel channel in channelList)
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channel.Id}");
    }
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    connectionUser.TryRemove(Context.ConnectionId, out _);

    // TODO: Add User status methods here

    await base.OnDisconnectedAsync(exception);
  }


  public async Task ChangeChannelMemberStatus(AppHubRecords.MemberStatusChange memberStatusChange)
  {
    User? user = GetCurrentUserOrAbort();
    if (user == null) return;

    await Clients.Group($"channel:{memberStatusChange.Id}").SendAsync(AppHubRecords.ClientMethods.ChannelMemberChanged,
        new AppHubRecords.MemberStatusChange(user.Id, memberStatusChange.ChangeType));
    if (memberStatusChange.ChangeType == AppHubRecords.ChangeType.JOINED)
    {
      bool isMember = await _dbContext.Channels.AnyAsync(c => c.Id == memberStatusChange.Id && c.Members.Any(m => m.Id == user.Id));
      if (!isMember) throw new HubException(AppHubRecords.Errors.NotChannelMember);

      await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");
    }
    else
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");
  }

  public async Task ChangeServerMemberStatus(AppHubRecords.MemberStatusChange memberStatusChange)
  {
    User? user = GetCurrentUserOrAbort();
    if (user == null) return;

    if (memberStatusChange.ChangeType == AppHubRecords.ChangeType.JOINED)
    {
      bool isMember = await _dbContext.Servers.AnyAsync(s => s.Id == memberStatusChange.Id && s.Members.Any(m => m.Id == user.Id));
      if (!isMember) throw new HubException(AppHubRecords.Errors.NotServerMember);

      await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");
    }
    else
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");

    List<Channel> channels = [.. _dbContext.Channels.Where(c => c.ServerId == memberStatusChange.Id
          && c.Members.Any(m => m.Id == user.Id))];

    foreach (Channel channel in channels)
    {
      await ChangeServerMemberStatus(new AppHubRecords.MemberStatusChange(channel.Id, memberStatusChange.ChangeType));
    }
  }
}
