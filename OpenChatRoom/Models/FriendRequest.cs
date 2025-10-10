using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class FriendRequest
{

    public FriendRequest() { }
    
    public FriendRequest(User author, User receiver)
    {
        Author = author;
        AuthorId = author.Id;
        Receiver = receiver;
        ReceiverId = receiver.Id;
        IsAccepted = false;
    }

    [Required]
    [StringLength(32)]
    [Column("author")]
    public string AuthorId { get; set; }
    public User Author { get; set; }

    [Required]
    [StringLength(32)]
    [Column("receiver")]
    public string ReceiverId { get; set; }
    public User Receiver { get; set; }

    [Required]
    [Column("is_accepted")]
    public bool IsAccepted { get; set; } = false;
}