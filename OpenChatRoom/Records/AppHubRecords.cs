public abstract class AppHubRecords
{
  public abstract class ClientMethods
  {
    // Data storage change methods
    public const string ChannelMemberChanged = "ChannelMemberChanged";
    public const string ServerMemberChanged = "ServerMemberChanged";

    // Automatic callback methods
    public const string ChangeChannelMemberStatus = "ChangeChannelMemberStatus";
    public const string ChangeServerMemberStatus = "ChangeServerMemberStatus";
  }

  public record MemberStatusChange(string Id, ChangeType ChangeType);

  public enum ChangeType
  {
    JOINED,
    LEFT
  }
}
