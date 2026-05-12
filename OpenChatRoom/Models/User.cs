using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{

  [Required]
  [StringLength(32)]
  [Column("id")]
  public string Id { get; set; } = IdGenerator.generateId();

  [Required]
  [StringLength(32)]
  [Column("username")]
  public string Username { get; set; } = null!;

  [Required]
  [StringLength(64)]
  [Column("visibleName")]
  public string VisibleName { get; set; } = null!;

  [Required]
  [StringLength(512)]
  [Column("verifier")]
  public string Verifier { get; set; } = null!;

  [Required]
  [StringLength(512)]
  [Column("salt")]
  public string Salt { get; set; } = null!;

  [Required]
  [Column("isAdmin")]
  public bool IsAdmin { get; set; } = false;

  [Column("lastTokenPurge")]
  public DateTime? LastTokenPurge { get; set; }

  // Set foreign keys here
  public List<Server> Servers { get; } = [];
  public List<Message> Messages { get; } = [];
  public List<Channel> PrivateChannels { get; } = [];
  public List<FriendRequest> SentRequests { get; } = [];
  public List<FriendRequest> ReceivedRequests { get; } = [];
}
