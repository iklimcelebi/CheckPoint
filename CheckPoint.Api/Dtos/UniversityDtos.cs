using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Validation;

namespace CheckPoint.Api.Dtos;

/// <summary>Represents the data required to create or update a university.</summary>
public class UniversityRequest
{
    [Required(ErrorMessage = "Üniversite adı zorunludur.")]
    [TrimmedStringLength(200, MinimumLength = 2, ErrorMessage = "Üniversite adı 2 ile 200 karakter arasında olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şehir zorunludur.")]
    [TrimmedStringLength(100, MinimumLength = 2, ErrorMessage = "Şehir 2 ile 100 karakter arasında olmalıdır.")]
    public string City { get; set; } = string.Empty;
}

/// <summary>Represents a university returned by the API.</summary>
public record UniversityResponse(int Id, string Name, string City);
