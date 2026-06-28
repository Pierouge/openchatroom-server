public abstract class AppHubRecords
{
  public abstract class ClientMethods
  {
    // Data storage change methods
    public const string ChannelMemberChanged = "ChannelMemberChanged"; //MemberStatusChange
    public const string ChannelDataChanged = "ChannelDataChanged";

    public const string ServerMemberChanged = "ServerMemberChanged"; //MemberStatusChange
    public const string ServerDataChanged = "ServerDataChanged";

    public const string FriendStatusChanged = "FriendStatusChanged"; // Takes a FriendRequest model as arg
    public const string FriendRemoved = "FriendRemoved"; // string containing userid (the other user than receiving)

    public const string UserDeleted = "UserDeleted"; // string containing userid

    // Automatic callback methods
    public const string ChangeChannelMemberStatus = "ChangeChannelMemberStatus";
    public const string ChangeServerMemberStatus = "ChangeServerMemberStatus";
  }

  public abstract class ServerMethods
  {
    public const string ChangeChannelMemberStatus = "ChangeChannelMemberStatus";
    public const string ChangeServerMemberStatus = "ChangeServerMemberStatus";
  }

  public abstract class Errors
  {
    public const string NotServerMember = "User is not member of the server";
    public const string NotChannelMember = "User is not member of the channel";
  }

  public record MemberStatusChange(string Id, ChangeType ChangeType);

  public enum ChangeType
  {
    JOINED,
    LEFT
  }
}
