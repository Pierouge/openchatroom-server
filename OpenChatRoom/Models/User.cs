using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{

  public User() { }

  public User(string username, string visibleName, string salt, string verifier)
  {
    Username = username;
    VisibleName = visibleName;
    Salt = salt;
    Verifier = verifier;
  }

  [Required]
  [StringLength(32)]
  [Column("id")]
  public string Id { get; set; } = IdGenerator.generateId();

  [Required]
  [StringLength(32)]
  [Column("username")]
  [NotNullOrWhiteSpace]
  [UsernameFormat]
  public string Username { get; set; } = null!;

  [Required]
  [StringLength(64)]
  [Column("visibleName")]
  [NotNullOrWhiteSpace]
  public string VisibleName { get; set; } = null!;

  [Required]
  [StringLength(512)]
  [Column("verifier")]
  [NotNullOrWhiteSpace]
  public string Verifier { get; set; } = null!;

  [Required]
  [StringLength(512)]
  [Column("salt")]
  [NotNullOrWhiteSpace]
  public string Salt { get; set; } = null!;

  [Required]
  [Column("isAdmin")]
  public bool IsAdmin { get; set; } = false;

  // Set foreign keys here
  public List<Server> Servers { get; } = [];
  public List<Server> OwnedServers { get; } = [];
  public List<Message> Messages { get; } = [];
  public List<Channel> PrivateChannels { get; } = [];
  public List<FriendRequest> SentRequests { get; } = [];
  public List<FriendRequest> ReceivedRequests { get; } = [];
  public List<UserToken> UserTokens { get; } = [];

  // Public info about User
  public record UserInfo(
      string Id,
      string Username,
      string VisibleName,
      bool IsAdmin);

  public UserInfo GetInfo()
  {
    return new(Id, Username, VisibleName, IsAdmin);
  }
}
