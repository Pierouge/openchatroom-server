using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize(AuthorizationType.Authenticated)]
public class AppHub(UserAccessor userAccessor, AppDbContext dbContext) : Hub
{
  private readonly UserAccessor _accessor = userAccessor;
  private readonly AppDbContext _dbContext = dbContext;

  private User? GetCurrentUserOrAbort()
  {
    var http = Context.GetHttpContext();
    if (http == null)
    {
      Context.Abort();
      return null;
    }

    User user = _accessor.GetCurrentUser(http)!;

    return user;
  }


  public override async Task OnConnectedAsync()
  {
    User? user = GetCurrentUserOrAbort();
    if (user == null) return;

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

  public async Task ChangeChannelMemberStatus(AppHubRecords.MemberStatusChange memberStatusChange)
  {
    User? user = GetCurrentUserOrAbort();
    if (user == null) return;

    await Clients.Group($"channel:{memberStatusChange.Id}").SendAsync(AppHubRecords.ClientMethods.ChannelMemberChanged,
        new AppHubRecords.MemberStatusChange(user.Id, memberStatusChange.ChangeType));
    if (memberStatusChange.ChangeType == AppHubRecords.ChangeType.JOINED)
      await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");
    else
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");
  }

  public async Task ChangeServerMemberStatus(AppHubRecords.MemberStatusChange memberStatusChange)
  {
    User? user = GetCurrentUserOrAbort();
    if (user == null) return;

    await Clients.Group($"server:{memberStatusChange.Id}").SendAsync(AppHubRecords.ClientMethods.ServerMemberChanged,
        new AppHubRecords.MemberStatusChange(user.Id, memberStatusChange.ChangeType));
    if (memberStatusChange.ChangeType == AppHubRecords.ChangeType.JOINED)
      await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");
    else
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{memberStatusChange.Id}");

    if (memberStatusChange.ChangeType == AppHubRecords.ChangeType.JOINED)
    {
      // TODO: Switch this line to default perm-based channel choice
      List<Channel> channels = [.. _dbContext.Channels.Where(c => c.ServerId == memberStatusChange.Id)];
      foreach (Channel channel in channels)
      {
        await ChangeServerMemberStatus(new AppHubRecords.MemberStatusChange(channel.Id, AppHubRecords.ChangeType.JOINED));
      }
    }
    else
    {
      List<Channel> channels = [.. _dbContext.Channels.Where(c => c.ServerId == memberStatusChange.Id && c.Members.Any(m => m.Id == user.Id))];
      foreach (Channel channel in channels)
      {
        await ChangeServerMemberStatus(new AppHubRecords.MemberStatusChange(channel.Id, AppHubRecords.ChangeType.LEFT));
      }
    }
  }
}
