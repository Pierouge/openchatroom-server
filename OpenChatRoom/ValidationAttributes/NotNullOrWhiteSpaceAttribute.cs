
using System.ComponentModel.DataAnnotations;

public class NotNullOrWhiteSpaceAttribute : ValidationAttribute
{
  protected override ValidationResult? IsValid(object? value, ValidationContext context)
  {
    if (value is string s && !string.IsNullOrWhiteSpace(s))
      return ValidationResult.Success;

    return new ValidationResult($"{context.MemberName} cannot be null, empty, or whitespace.");
  }
}
