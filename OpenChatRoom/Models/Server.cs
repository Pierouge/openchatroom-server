using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Server
{
  [Required]
  [StringLength(32)]
  [Column("id")]
  public string Id { get; set; } = Guid.NewGuid().ToString("N");

  [Required]
  [StringLength(64)]
  [Column("name")]
  public string Name { get; set; } = null!;

  [StringLength(32)]
  [Column("ownerId")]
  public string? OwnerId { get; set; }

  [JsonIgnore]
  public User? Owner { get; set; }

  [JsonIgnore]
  public List<User> Members { get; } = [];

  [JsonIgnore]
  public List<Channel> Channels { get; } = [];

  [Timestamp]
  [JsonIgnore]
  [Column("RowVersion")]
  public byte[] RowVersion { get; set; } = [];
}
