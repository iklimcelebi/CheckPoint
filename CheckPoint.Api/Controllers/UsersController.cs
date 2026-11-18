using System.ComponentModel.DataAnnotations;
using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Extensions;
using CheckPoint.Api.Services;
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
    private const int TopResourceCount = 3;

    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext db, IFileStorageService fileStorage, ILogger<UsersController> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [HttpGet("me/stats")]
    [EndpointSummary("Giriş yapmış kullanıcının kaynak istatistiklerini getirir")]
    [EndpointDescription(
        "Yüklenen kaynak sayısı, bu kaynakların toplam görüntülenmesi, başkalarınca favoriye eklenme sayısı, " +
        "kullanıcının kendi favori sayısı, türe göre dağılım ve en çok görüntülenen kaynaklar.")]
    [ProducesResponseType(typeof(UserStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserStatsResponse>> MyStats(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var mine = _db.EducationalResources.AsNoTracking().Where(resource => resource.UploadedByUserId == userId);

        var byType = await mine
            .GroupBy(resource => resource.Type)
            .Select(group => new { Type = group.Key, Count = group.Count(), Views = group.Sum(resource => resource.ViewCount) })
            .ToListAsync(cancellationToken);
        var favoritesReceived = await _db.Favorites
            .CountAsync(favorite => favorite.Resource!.UploadedByUserId == userId, cancellationToken);
        var favoriteCount = await _db.Favorites.CountAsync(favorite => favorite.UserId == userId, cancellationToken);
        var topResources = await mine
            .Where(resource => resource.ViewCount > 0)
            .OrderByDescending(resource => resource.ViewCount)
            .ThenByDescending(resource => resource.UploadedAt)
            .Take(TopResourceCount)
            .Select(resource => new TopResourceResponse(resource.Id, resource.Title, resource.ViewCount))
            .ToListAsync(cancellationToken);

        return new UserStatsResponse(
            byType.Sum(group => group.Count),
            byType.Sum(group => group.Views),
            favoritesReceived,
            favoriteCount,
            byType
                .OrderBy(group => group.Type)
                .Select(group => new ResourceTypeCountResponse(group.Type, group.Count))
                .ToList(),
            topResources);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Kullanıcıları listeler (Admin)")]
    [EndpointDescription("Kullanıcı adına göre sıralı, sayfalı liste. q verilirse kullanıcı adı veya e-posta içinde arar.")]
    [ProducesResponseType(typeof(PagedResponse<AdminUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AdminUserResponse>>> GetAll(
        [FromQuery, StringLength(100, ErrorMessage = "Arama metni en fazla 100 karakter olabilir.")] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!Pagination.IsValid(page, pageSize))
            return Problem(detail: Pagination.InvalidPagingMessage, statusCode: StatusCodes.Status400BadRequest);

        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(user =>
                EF.Functions.Collate(user.Username, SearchCollation.CaseAndAccentInsensitive).Contains(search) ||
                EF.Functions.Collate(user.Email, SearchCollation.CaseAndAccentInsensitive).Contains(search));
        }

        return await query
            .OrderBy(user => user.Username)
            .ThenBy(user => user.Id)
            .Select(user => new AdminUserResponse(
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.UploadedResources.Count))
            .ToPagedResponseAsync(page, pageSize, cancellationToken);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Kullanıcı hesabını siler (Admin)")]
    [EndpointDescription(
        "Hesapla birlikte kullanıcının yüklediği kaynaklar, dosyaları ve favorileri silinir. Yönetici hesapları " +
        "ve yöneticinin kendi hesabı silinemez (409). Depolama erişilemezse hiçbir şey silinmez ve 503 döner.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
            return Problem(detail: "Kullanıcı bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        if (user.Id == User.GetUserId())
            return Problem(detail: "Kendi hesabınızı silemezsiniz.", statusCode: StatusCodes.Status409Conflict);

        if (user.Role == "Admin")
            return Problem(detail: "Yönetici hesapları silinemez.", statusCode: StatusCodes.Status409Conflict);

        var resources = await _db.EducationalResources
            .Where(resource => resource.UploadedByUserId == id)
            .ToListAsync(cancellationToken);

        var stagedFiles = new List<IStagedFileDeletion>();
        try
        {
            foreach (var resource in resources)
                stagedFiles.Add(await _fileStorage.StageDeleteAsync(resource.StoredFileName, cancellationToken));

            _db.EducationalResources.RemoveRange(resources);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(CancellationToken.None);
        }
        catch
        {
            foreach (var staged in stagedFiles)
            {
                try
                {
                    await staged.RestoreAsync(CancellationToken.None);
                }
                catch (Exception restoreException)
                {
                    _logger.LogError(restoreException, "Could not restore a stored file after deleting user {UserId} failed.", id);
                }
            }

            throw;
        }

        foreach (var staged in stagedFiles)
        {
            try
            {
                await staged.CompleteAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "User {UserId} was deleted but a stored file could not be removed.", id);
            }
        }

        return NoContent();
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
            userId,
            cancellationToken));
    }

    [HttpGet("me/favorites")]
    [EndpointSummary("Giriş yapmış kullanıcının favori kaynaklarını listeler")]
    [EndpointDescription("En son favorilere eklenen önce gelir.")]
    [ProducesResponseType(typeof(PagedResponse<ResourceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ResourceResponse>>> MyFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!Pagination.IsValid(page, pageSize))
            return Problem(detail: Pagination.InvalidPagingMessage, statusCode: StatusCodes.Status400BadRequest);

        var userId = User.GetUserId();
        return Ok(await _db.Favorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.ResourceId)
            .Select(favorite => favorite.Resource!)
            .Select(ResourcesController.ResourceProjection(userId))
            .ToPagedResponseAsync(page, pageSize, cancellationToken));
    }
}
