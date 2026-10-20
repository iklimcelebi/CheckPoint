using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CoursesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CourseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseResponse>>> GetAll(
        [FromQuery] int? universityId,
        CancellationToken cancellationToken)
    {
        var query = _db.Courses.AsNoTracking();
        if (universityId.HasValue)
            query = query.Where(course => course.UniversityId == universityId.Value);

        var courses = await query
            .OrderBy(course => course.Name)
            .Select(course => new CourseResponse(
                course.Id,
                course.Name,
                course.Code,
                course.UniversityId,
                course.University!.Name))
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var course = await _db.Courses
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new CourseResponse(
                candidate.Id,
                candidate.Name,
                candidate.Code,
                candidate.UniversityId,
                candidate.University!.Name))
            .FirstOrDefaultAsync(cancellationToken);

        if (course is null)
            return Problem(detail: "Ders bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        return Ok(course);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseResponse>> Create(
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        var universityExists = await _db.Universities
            .AnyAsync(university => university.Id == request.UniversityId, cancellationToken);
        if (!universityExists)
            return Problem(detail: "Üniversite bulunamadı.", statusCode: StatusCodes.Status400BadRequest);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Courses.AnyAsync(
                course => course.UniversityId == request.UniversityId && course.Code == code,
                cancellationToken))
        {
            return Problem(
                detail: "Bu üniversitede aynı ders koduyla kayıtlı bir ders zaten var.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var course = new Course
        {
            Name = request.Name.Trim(),
            Code = code,
            UniversityId = request.UniversityId
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(cancellationToken);

        var universityName = await _db.Universities
            .Where(university => university.Id == course.UniversityId)
            .Select(university => university.Name)
            .SingleAsync(cancellationToken);
        var response = new CourseResponse(
            course.Id,
            course.Name,
            course.Code,
            course.UniversityId,
            universityName);

        return CreatedAtAction(nameof(GetById), new { id = course.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        CourseUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var course = await _db.Courses.FindAsync([id], cancellationToken);
        if (course is null)
            return Problem(detail: "Ders bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Courses.AnyAsync(
                candidate => candidate.UniversityId == course.UniversityId
                    && candidate.Code == code
                    && candidate.Id != id,
                cancellationToken))
        {
            return Problem(
                detail: "Bu üniversitede aynı ders koduyla kayıtlı bir ders zaten var.",
                statusCode: StatusCodes.Status409Conflict);
        }

        course.Name = request.Name.Trim();
        course.Code = code;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
