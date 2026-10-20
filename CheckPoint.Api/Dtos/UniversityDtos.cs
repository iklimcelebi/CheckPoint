using System.ComponentModel.DataAnnotations;

namespace CheckPoint.Api.Dtos;

/// <summary>Represents the data required to create or update a university.</summary>
public class UniversityRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;
}

/// <summary>Represents a university returned by the API.</summary>
public record UniversityResponse(int Id, string Name, string City);
