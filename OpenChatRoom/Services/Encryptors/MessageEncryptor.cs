using Microsoft.AspNetCore.DataProtection;

public class MessageEncryptor(IDataProtectionProvider provider)
{
  private readonly IDataProtector _protector = provider.CreateProtector("OpenChatRoom.Messages");

  public string Encrypt(string plaintext)
  {
    return _protector.Protect(plaintext);
  }

  public string Decrypt(string ciphertext)
  {
    return _protector.Unprotect(ciphertext);
  }
}
