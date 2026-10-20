using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Models;

namespace CheckPoint.Api.Dtos;

/// <summary>Multipart form data required to upload an educational resource.</summary>
public sealed class CreateResourceRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int UniversityId { get; set; }

    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [EnumDataType(typeof(ResourceType))]
    public ResourceType Type { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}

/// <summary>Fields that can be changed for an existing educational resource.</summary>
public sealed record UpdateResourceRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [EnumDataType(typeof(ResourceType))]
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
    DateTimeOffset UploadedAt);

/// <summary>A page of educational resources.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
