using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Validation;

namespace CheckPoint.Api.Dtos;

/// <summary>Public profile details for a user.</summary>
public sealed record UserResponse(int Id, string Username, string Email, string Role);

/// <summary>Current and new password for changing the signed-in user's password.</summary>
public sealed record ChangePasswordRequest
{
    [Required(ErrorMessage = "Mevcut parola zorunludur.")]
    [StringLength(1024, ErrorMessage = "Parola çok uzun.")]
    public required string CurrentPassword { get; init; }

    [Required(ErrorMessage = "Yeni parola zorunludur.")]
    [StringLength(PasswordPolicy.MaximumLength, MinimumLength = PasswordPolicy.MinimumLength,
        ErrorMessage = PasswordPolicy.LengthMessage)]
    [PasswordPolicy]
    public required string NewPassword { get; init; }
}
