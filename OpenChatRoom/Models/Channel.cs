using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class Channel
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
  [Column("server")]
  public string? ServerId { get; set; }

  [JsonIgnore]
  public Server? Server { get; set; }

  [Required]
  [Column("lastMessage")]
  public DateTime LastMessage { get; set; } = DateTime.Now;

  [JsonIgnore]
  public List<Message> Messages { get; } = [];

  [JsonIgnore]
  public List<User> Members { get; } = [];
}

