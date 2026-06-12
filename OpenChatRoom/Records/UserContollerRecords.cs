using System.ComponentModel.DataAnnotations;

public abstract class UserControllerRecords
{
  // Create Request
  public record CreateUserRequest(
    [NotNullOrWhiteSpace][UsernameFormat][StringLength(32)] string Username,
    [NotNullOrWhiteSpace][StringLength(64)] string VisibleName,
    [NotNullOrWhiteSpace] string Salt,
    [NotNullOrWhiteSpace] string Verifier
  );

  public record CreateResult(
      User.UserInfo UserInfo,
      TokenPair TokenPair
  );

  // SRP Phase 1
  public record SrpStep1Request(
    [NotNullOrWhiteSpace] string Username,
    [NotNullOrWhiteSpace] string ClientEphemeralPublic
  );
  public record SrpStep2Response(
    string Salt,
    string ServerPublicEphemeral,
    string Token
  );

  // SRP Phase 2
  public record SrpStep4Response(
    string Proof,
    TokenPair TokenPair
  );
}
