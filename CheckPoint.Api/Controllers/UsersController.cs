using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    [EndpointSummary("Giriş yapmış kullanıcının bilgilerini getirir")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
            return Problem(detail: "Kullanıcı bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        return new UserResponse(user.Id, user.Username, user.Email, user.Role);
    }

    [HttpGet("me/resources")]
    [EndpointSummary("Giriş yapmış kullanıcının yüklediği kaynakları listeler")]
    [ProducesResponseType(typeof(PagedResponse<ResourceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ResourceResponse>>> MyResources(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!Pagination.IsValid(page, pageSize))
            return Problem(detail: Pagination.InvalidPagingMessage, statusCode: StatusCodes.Status400BadRequest);

        var userId = User.GetUserId();
        var query = _db.EducationalResources
            .AsNoTracking()
            .Where(resource => resource.UploadedByUserId == userId);

        return Ok(await ResourcesController.ToPagedResponseAsync(
            query,
            page,
            pageSize,
            cancellationToken));
    }
}
