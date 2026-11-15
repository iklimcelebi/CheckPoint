using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Validation;

namespace CheckPoint.Api.Dtos;

/// <summary>Data required to create a course for a university.</summary>
public sealed record CourseRequest
{
    [Required(ErrorMessage = "Ders adı zorunludur.")]
    [TrimmedStringLength(200, MinimumLength = 2, ErrorMessage = "Ders adı 2 ile 200 karakter arasında olmalıdır.")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "Ders kodu zorunludur.")]
    [TrimmedStringLength(20, MinimumLength = 1, ErrorMessage = "Ders kodu 1 ile 20 karakter arasında olmalıdır.")]
    public required string Code { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir üniversite seçin.")]
    public int UniversityId { get; init; }
}

/// <summary>Data that can be changed when updating a course.</summary>
public sealed record CourseUpdateRequest
{
    [Required(ErrorMessage = "Ders adı zorunludur.")]
    [TrimmedStringLength(200, MinimumLength = 2, ErrorMessage = "Ders adı 2 ile 200 karakter arasında olmalıdır.")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "Ders kodu zorunludur.")]
    [TrimmedStringLength(20, MinimumLength = 1, ErrorMessage = "Ders kodu 1 ile 20 karakter arasında olmalıdır.")]
    public required string Code { get; init; }
}

/// <summary>Course details returned by the API.</summary>
public sealed record CourseResponse(
    int Id,
    string Name,
    string Code,
    int UniversityId,
    string UniversityName);
