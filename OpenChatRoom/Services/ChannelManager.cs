public static class ChannelManager
{
  public static List<User> FindChannelMembers(Channel channel)
  {
    return channel.Members;
  }

  public static List<Message> GetChannelMessages(AppDbContext _context, Channel channel, int limit, int page)
  {
    return [.. _context.Messages.Where(m => m.Channel == channel).OrderByDescending(m => m.Time)
        .Skip((page - 1) * limit).Take(limit)];
  }
}
