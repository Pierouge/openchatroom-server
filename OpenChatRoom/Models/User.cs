using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{

    [Required]
    [StringLength(32)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [StringLength(32)]
    [Column("username")]
    public string Username { get; set; }

    [Required]
    [StringLength(64)]
    [Column("visibleName")]
    public string VisibleName { get; set; }

    [Required]
    [StringLength(512)]
    [Column("verifier")]
    public string Verifier { get; set; }

    [Required]
    [StringLength(512)]
    [Column("salt")]
    public string Salt { get; set; }

    [Required]
    [Column("isAdmin")]
    public bool IsAdmin { get; set; } = false;

    // Set foreign keys here
    public RefreshToken? RefreshToken { get; set; }
    public List<Server> Servers { get; } = [];
    public List<Message> Messages { get; } = [];

}