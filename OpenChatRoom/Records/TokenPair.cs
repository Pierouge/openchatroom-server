public class TokenPair(string Token, string RefreshToken)
{
  public string Token { get; set; } = Token;

  public string RefreshToken { get; set; } = RefreshToken;

  public static TokenPair FromJWTServiceResult(JWTBuilder.Result result)
  {
    if (result.RefreshToken == null)
      throw new NullReferenceException("refreshToken can't be null");

    return new TokenPair(result.Token, result.RefreshToken);
  }
}
