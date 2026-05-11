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
  public string AuthorId { get; set; } = null!;
  public User Author { get; set; } = null!;

  [Required]
  [StringLength(32)]
  [Column("receiver")]
  public string ReceiverId { get; set; } = null!;
  public User Receiver { get; set; } = null!;

  [Required]
  [Column("isAccepted")]
  public bool IsAccepted { get; set; } = false;
}
