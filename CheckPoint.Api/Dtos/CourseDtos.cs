using System.ComponentModel.DataAnnotations;

namespace CheckPoint.Api.Dtos;

/// <summary>Data required to create a course for a university.</summary>
public sealed record CourseRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required, StringLength(20, MinimumLength = 1)]
    public required string Code { get; init; }

    [Range(1, int.MaxValue)]
    public int UniversityId { get; init; }
}

/// <summary>Data that can be changed when updating a course.</summary>
public sealed record CourseUpdateRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required, StringLength(20, MinimumLength = 1)]
    public required string Code { get; init; }
}

/// <summary>Course details returned by the API.</summary>
public sealed record CourseResponse(
    int Id,
    string Name,
    string Code,
    int UniversityId,
    string UniversityName);
