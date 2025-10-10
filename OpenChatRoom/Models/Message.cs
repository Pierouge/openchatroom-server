using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class Message
{
    [Required]
    [StringLength(32)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [StringLength(2048)]
    [Column("text")]
    public string Text { get; set; }

    [Required]
    [Column("time")]
    public DateTime Time { get; set; }

    [Required]
    [Column("is_modified")]
    public bool IsModified { get; set; } = false;

    [Required]
    [StringLength(32)]
    [Column("author")]
    public string AuthorId { get; set; }
    public User? Author { get; set; }

    [Required]
    [StringLength(32)]
    [Column("channel")]
    public string ChannelId { get; set; }
    public Channel Channel { get; set; }
}