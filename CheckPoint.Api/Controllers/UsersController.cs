using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    [HttpPut("me/password")]
    [EnableRateLimiting(SecurityServiceExtensions.AuthRateLimitPolicy)]
    [EndpointSummary("Giriş yapmış kullanıcının parolasını değiştirir")]
    [EndpointDescription(
        "Mevcut parola doğrulanır; yeni parola kayıt ile aynı kurala uymalıdır (8-64 karakter, en az bir harf " +
        "ve bir rakam). Mevcut parola yanlışsa 400 döner. Açık oturumlar geçerliliğini korur.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
            return Problem(detail: "Kullanıcı bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return ApiBehaviorExtensions.ValidationProblemResult(HttpContext, new Dictionary<string, string[]>
            {
                [nameof(ChangePasswordRequest.CurrentPassword)] = ["Mevcut parola hatalı."]
            });
        }

        if (request.NewPassword == request.CurrentPassword)
        {
            return ApiBehaviorExtensions.ValidationProblemResult(HttpContext, new Dictionary<string, string[]>
            {
                [nameof(ChangePasswordRequest.NewPassword)] = ["Yeni parola mevcut paroladan farklı olmalıdır."]
            });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
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
