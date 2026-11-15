using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CheckPoint.Api.Validation;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 64;
    public const int MaximumBytes = 72;

    public const string LengthMessage = "Parola 8 ile 64 karakter arasında olmalıdır.";
    public const string LetterAndDigitMessage = "Parola en az bir harf ve bir rakam içermelidir.";
    public const string ByteLengthMessage =
        "Parola çok uzun: ğ, ü, ş gibi Türkçe karakterler 2 bayt sayıldığından parola en fazla 72 bayt olabilir.";

    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length is < MinimumLength or > MaximumLength)
            return LengthMessage;

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return LetterAndDigitMessage;

        if (Encoding.UTF8.GetByteCount(password) > MaximumBytes)
            return ByteLengthMessage;

        return null;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordPolicyAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || password.Length is < PasswordPolicy.MinimumLength or > PasswordPolicy.MaximumLength)
            return ValidationResult.Success;

        var error = PasswordPolicy.Validate(password);
        return error is null ? ValidationResult.Success : new ValidationResult(error, [validationContext.MemberName!]);
    }
}
