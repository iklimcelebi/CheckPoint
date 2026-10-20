namespace CheckPoint.Api.Models;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public int UniversityId { get; set; }
    public University? University { get; set; }
    public List<EducationalResource> Resources { get; set; } = new();
}