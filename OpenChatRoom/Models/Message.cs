using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
public class Message
{

  public Message() { }

  [Required]
  [StringLength(32)]
  [Column("id")]
  public string Id { get; set; } = Guid.NewGuid().ToString("N");

  [Required]
  [StringLength(2048)]
  [Column("text")]
  public string Text { get; set; } = null!;

  [Required]
  [Column("time")]
  public DateTime Time { get; set; } = DateTime.Now;

  [Required]
  [Column("isModified")]
  public bool IsModified { get; set; } = false;

  [Required]
  [StringLength(32)]
  [Column("author")]
  public string AuthorId { get; set; } = null!;

  [JsonIgnore]
  public User Author { get; set; } = null!;

  [Required]
  [StringLength(32)]
  [Column("channel")]
  public string ChannelId { get; set; } = null!;

  [JsonIgnore]
  public Channel Channel { get; set; } = null!;
}
