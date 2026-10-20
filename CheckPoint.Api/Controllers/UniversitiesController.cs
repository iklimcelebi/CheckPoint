using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UniversitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public UniversitiesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UniversityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UniversityResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return await _db.Universities
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UniversityResponse(u.Id, u.Name, u.City))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UniversityResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var university = await _db.Universities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (university is null)
            return Problem(detail: "Üniversite bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        return new UniversityResponse(university.Id, university.Name, university.City);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UniversityResponse>> Create(
        UniversityRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _db.Universities.AnyAsync(u => u.Name == name, cancellationToken))
            return Problem(detail: "Bu isimde bir üniversite zaten var.", statusCode: StatusCodes.Status409Conflict);

        var university = new University { Name = name, City = request.City.Trim() };
        _db.Universities.Add(university);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = university.Id },
            new UniversityResponse(university.Id, university.Name, university.City));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        UniversityRequest request,
        CancellationToken cancellationToken)
    {
        var university = await _db.Universities.FindAsync([id], cancellationToken);
        if (university is null)
            return Problem(detail: "Üniversite bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        var name = request.Name.Trim();
        if (await _db.Universities.AnyAsync(u => u.Name == name && u.Id != id, cancellationToken))
            return Problem(detail: "Bu isimde bir üniversite zaten var.", statusCode: StatusCodes.Status409Conflict);

        university.Name = name;
        university.City = request.City.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var university = await _db.Universities.FindAsync([id], cancellationToken);
        if (university is null)
            return Problem(detail: "Üniversite bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        if (await _db.Courses.AnyAsync(c => c.UniversityId == id, cancellationToken))
            return Problem(detail: "Bu üniversiteye bağlı dersler var, önce onlar silinmeli.",
                statusCode: StatusCodes.Status409Conflict);

        _db.Universities.Remove(university);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
