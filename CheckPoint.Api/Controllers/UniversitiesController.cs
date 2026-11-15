using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Extensions;
using CheckPoint.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class UniversitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public UniversitiesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [EndpointSummary("Üniversiteleri listeler")]
    [EndpointDescription("Ada göre sıralı, sayfalı liste döner. q verilirse ad veya şehir içinde arar.")]
    [ProducesResponseType(typeof(PagedResponse<UniversityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<UniversityResponse>>> GetAll(
        [FromQuery, StringLength(100, ErrorMessage = "Arama metni en fazla 100 karakter olabilir.")] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!Pagination.IsValid(page, pageSize))
            return Problem(detail: Pagination.InvalidPagingMessage, statusCode: StatusCodes.Status400BadRequest);

        var query = _db.Universities.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(u => u.Name.Contains(search) || u.City.Contains(search));
        }

        return await query
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Select(u => new UniversityResponse(u.Id, u.Name, u.City))
            .ToPagedResponseAsync(page, pageSize, cancellationToken);
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Üniversite ayrıntısını getirir")]
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
    [EndpointSummary("Üniversite ekler (Admin)")]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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
    [EndpointSummary("Üniversiteyi günceller (Admin)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
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
    [EndpointSummary("Üniversiteyi siler (Admin)")]
    [EndpointDescription("Dersi olan üniversite silinemez; bu durumda 409 döner.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var university = await _db.Universities.FindAsync([id], cancellationToken);
        if (university is null)
            return Problem(detail: "Üniversite bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        if (await _db.Courses.AnyAsync(c => c.UniversityId == id, cancellationToken))
            return Problem(detail: "Bu üniversiteye bağlı dersler olduğu için silinemez.",
                statusCode: StatusCodes.Status409Conflict);

        _db.Universities.Remove(university);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
