namespace CheckPoint.Api.Models;

public class EducationalResource
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UniversityId { get; set; }
    public int CourseId { get; set; }
    public ResourceType Type { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public University? University { get; set; }
    public Course? Course { get; set; }
    public User? UploadedByUser { get; set; }
}
