using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class UserToken
{
  public UserToken() { }

  public UserToken(string id, string refreshId, User user, DateTime validTime)
  {
    Id = id;
    RefreshId = refreshId;
    UserId = user.Id;
    User = user;
    ValidTime = validTime;
  }

  [Required]
  [StringLength(32)]
  [Column("id")]
  public string Id { get; set; } = null!;

  [Required]
  [StringLength(32)]
  [Column("refreshId")]
  public string RefreshId { get; set; } = null!;

  [Required]
  [StringLength(32)]
  [Column("userId")]
  public string UserId { get; set; } = null!;

  [JsonIgnore]
  public User User { get; set; } = null!;

  [Required]
  [Column("validTime")]
  public DateTime ValidTime { get; set; }

  [Timestamp]
  [JsonIgnore]
  [Column("RowVersion")]
  public byte[] RowVersion { get; set; } = [];
}
