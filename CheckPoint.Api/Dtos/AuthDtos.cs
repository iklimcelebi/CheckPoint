using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Validation;

namespace CheckPoint.Api.Dtos;

/// <summary>Credentials and profile details for registering a user.</summary>
public sealed record RegisterRequest
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3 ile 50 karakter arasında olmalıdır.")]
    [RegularExpression(@"^\S(?:.*\S)?$", ErrorMessage = "Kullanıcı adının başında veya sonunda boşluk olamaz.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(256, ErrorMessage = "E-posta adresi en fazla 256 karakter olabilir.")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Parola zorunludur.")]
    [StringLength(PasswordPolicy.MaximumLength, MinimumLength = PasswordPolicy.MinimumLength,
        ErrorMessage = PasswordPolicy.LengthMessage)]
    [PasswordPolicy]
    public required string Password { get; init; }
}

/// <summary>Credentials used to sign in.</summary>
public sealed record LoginRequest
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(256, ErrorMessage = "E-posta adresi en fazla 256 karakter olabilir.")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Parola zorunludur.")]
    [StringLength(1024, ErrorMessage = "Parola çok uzun.")]
    public required string Password { get; init; }
}

/// <summary>Authentication token and the signed-in user's profile.</summary>
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, UserResponse User);
