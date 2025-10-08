using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

public class RefreshToken
{
    // Parameterless constructor required by EF Core
    public RefreshToken() { }

    public RefreshToken(User user)
    {
        User = user;
        UserId = user.Id;
        Token = generateToken(178);
        ExpiryTime = DateTime.Now.AddDays(15);
    }

    [Required]
    [StringLength(178)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    [Column("user")]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; }

    [Required]
    [Column("expiryTime")]
    public DateTime ExpiryTime { get; set; }

    private static string generateToken(int desiredLength)
    {
        const string dictionaryString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder resultStringBuilder = new();
        Random random = new();
        for (int i = 0; i < desiredLength; i++)
        {
            resultStringBuilder.Append(dictionaryString[random.Next(dictionaryString.Length)]);
        }
        return resultStringBuilder.ToString();
    }
}