using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Server
{
    [Required]
    [StringLength(32)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [StringLength(64)]
    [Column("name")]
    public string Name { get; set; }

    public List<User> Members { get; } = [];
    public List<Channel> Channels { get; } = [];
}