using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Models;
using CheckPoint.Api.Validation;

namespace CheckPoint.Api.Dtos;

/// <summary>Multipart form data required to upload an educational resource.</summary>
public sealed class CreateResourceRequest
{
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [TrimmedStringLength(200, MinimumLength = 1, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [TrimmedStringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir üniversite seçin.")]
    public int UniversityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ders seçin.")]
    public int CourseId { get; set; }

    [EnumDataType(typeof(ResourceType), ErrorMessage = "Kaynak türü geçersiz.")]
    public ResourceType Type { get; set; }

    [Required(ErrorMessage = "Yüklenecek dosya zorunludur.")]
    public IFormFile? File { get; set; }
}

/// <summary>Fields that can be changed for an existing educational resource.</summary>
public sealed record UpdateResourceRequest
{
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [TrimmedStringLength(200, MinimumLength = 1, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    public required string Title { get; init; }

    [TrimmedStringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; init; }

    [EnumDataType(typeof(ResourceType), ErrorMessage = "Kaynak türü geçersiz.")]
    public ResourceType Type { get; init; }
}

/// <summary>University details included with an educational resource.</summary>
public sealed record ResourceUniversityResponse(int Id, string Name);

/// <summary>Course details included with an educational resource.</summary>
public sealed record ResourceCourseResponse(int Id, string Name, string Code);

/// <summary>Uploader details included with an educational resource.</summary>
public sealed record ResourceUploaderResponse(int Id, string Username);

/// <summary>Details of an uploaded educational resource.</summary>
public sealed record ResourceResponse(
    int Id,
    string Title,
    string? Description,
    ResourceUniversityResponse University,
    ResourceCourseResponse Course,
    ResourceType Type,
    string OriginalFileName,
    long FileSize,
    ResourceUploaderResponse UploadedBy,
    DateTimeOffset UploadedAt,
    int ViewCount,
    bool IsFavorite);

/// <summary>A page of results.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
