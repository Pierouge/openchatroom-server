public static class ChannelManager
{
    public static List<User> findChannelMembers(Channel channel)
    {
        if (channel.Server != null)
            return channel.Server.Members;

        return channel.PrivateChannelMembers;
    }

    public static List<Message> getChannelMessages(AppDbContext _context, Channel channel, int limit, int page)
    {
        return _context.Messages.Where(m => m.Channel == channel).OrderByDescending(m => m.Time)
            .Skip((page-1) * limit).Take(limit).ToList();
    }
}