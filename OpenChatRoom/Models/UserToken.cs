using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserToken
{
  public UserToken() { }

  public UserToken(string id, User user, DateTime validTime)
  {
    Id = id;
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
  [Column("userId")]
  public string UserId { get; set; } = null!;
  public User User { get; set; } = null!;

  [Required]
  [Column("validTime")]
  public DateTime ValidTime { get; set; }
}
