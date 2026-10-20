namespace CheckPoint.Api.Models;

public class University
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public List<Course> Courses { get; set; } = new();
    public List<EducationalResource> Resources { get; set; } = new();
}