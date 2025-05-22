using System.ComponentModel.DataAnnotations.Schema;

public class User
{

    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("visibleName")]
    public string VisibleName { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;
    [Column("salt")]
    public string Salt { get; set; } = string.Empty;

}