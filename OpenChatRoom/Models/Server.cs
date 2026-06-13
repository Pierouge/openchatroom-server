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
  public string? AuthorId { get; set; }

  [JsonIgnore]
  public User? Author { get; set; }

  [JsonIgnore]
  public List<User> Members { get; } = [];

  [JsonIgnore]
  public List<Channel> Channels { get; } = [];
}
