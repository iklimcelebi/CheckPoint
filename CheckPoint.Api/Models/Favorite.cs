namespace CheckPoint.Api.Models;

public class Favorite
{
    public int UserId { get; set; }
    public int ResourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
    public EducationalResource? Resource { get; set; }
}
