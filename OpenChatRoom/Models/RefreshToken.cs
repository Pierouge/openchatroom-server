using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

public class RefreshToken
{
    [Required]
    [StringLength(178)]
    [Column("token")]
    public string Token { get; set; } = generateToken(178);
    [Required]
    [StringLength(32)]
    [Column("user")]
    public string User { get; set; }
    [Required]
    [Column("tokenTime")]
    public DateTime TokenTime = DateTime.Now.AddDays(15);

    public RefreshToken(string user)
    {
        User = user;
    }

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