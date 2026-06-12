using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class UsernameFormatAttribute : ValidationAttribute
{
  protected override ValidationResult? IsValid(object? value, ValidationContext context)
  {
    if (value is not string s)
      return new ValidationResult($"{context.MemberName} must be a string.");

    if (!UsernameRegexHelper.UsernameRegex().IsMatch(s))
      return new ValidationResult($"{context.MemberName} contains invalid characters.");

    return ValidationResult.Success;
  }

}

public static partial class UsernameRegexHelper
{
  [GeneratedRegex("^[a-z0-9]+$", RegexOptions.Compiled)]
  public static partial Regex UsernameRegex();
}

