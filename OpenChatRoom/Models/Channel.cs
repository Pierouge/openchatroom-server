using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Channel
{
    [Required]
    [StringLength(32)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [StringLength(64)]
    [Column("name")]
    public string Name { get; set; }

    [StringLength(32)]
    [Column("server")]
    public string? ServerId { get; set; }
    public Server? Server { get; set; }

    [Required]
    [Column("lastMessage")]
    public DateTime LastMessage { get; set; } = DateTime.Now;

    public List<Message> Messages { get; } = [];

    public List<User> PrivateChannelMembers { get; } = [];
}

