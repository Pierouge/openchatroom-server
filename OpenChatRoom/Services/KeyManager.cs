using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

/*
 * The KeyManager class forces the generation of a key in the selected file path if no acceptable key is found in said file
 */

public class KeyManager
{
  public static SymmetricSecurityKey GetOrGenKey(string path)
  {
    byte[] keyBytes;

    if (!File.Exists(path)) keyBytes = GenerateKey(path); // Check if file exists
    else
    {
      string hex = File.ReadAllText(path).Trim();
      try
      {
        byte[] bytes = Convert.FromHexString(hex);
        if (bytes.Length != 32) throw new InvalidOperationException("Key length mismatch");
        keyBytes = bytes;
      }
      catch (Exception) // Cannot convert from Hex or is not 32 bytes long
      {
        keyBytes = GenerateKey(path);
      }
    }

    return new SymmetricSecurityKey(keyBytes);
  }

  private static byte[] GenerateKey(string path)
  {
    byte[] bytes = RandomNumberGenerator.GetBytes(32);
    string hex = Convert.ToHexString(bytes).ToLowerInvariant();

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    File.WriteAllText(path, hex);
    return bytes;
  }

}
