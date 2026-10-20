using System.Linq.Expressions;
using System.Text;
using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Extensions;
using CheckPoint.Api.Models;
using CheckPoint.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Controllers;

[ApiController]
[Route("api/resources")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private const long BytesPerMegabyte = 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg"
        };

    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly long _maxFileSize;

    public ResourcesController(
        AppDbContext db,
        IFileStorageService fileStorage,
        IConfiguration configuration)
    {
        _db = db;
        _fileStorage = fileStorage;
        _maxFileSize = configuration.GetValue<int>("FileStorage:MaxFileSizeMB") * BytesPerMegabyte;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ResourceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ResourceResponse>>> GetAll(
        [FromQuery] int? universityId,
        [FromQuery] int? courseId,
        [FromQuery] ResourceType? type,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return Problem(detail: "Sayfa en az 1, sayfa boyutu 1 ile 100 arasında olmalıdır.",
                statusCode: StatusCodes.Status400BadRequest);

        var query = _db.EducationalResources.AsNoTracking();
        if (universityId.HasValue)
            query = query.Where(resource => resource.UniversityId == universityId.Value);
        if (courseId.HasValue)
            query = query.Where(resource => resource.CourseId == courseId.Value);
        if (type.HasValue)
            query = query.Where(resource => resource.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(resource =>
                resource.Title.Contains(search) ||
                (resource.Description != null && resource.Description.Contains(search)));
        }

        return Ok(await ToPagedResponseAsync(query, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var resource = await _db.EducationalResources
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(ResourceProjection())
            .FirstOrDefaultAsync(cancellationToken);

        if (resource is null)
            return Problem(detail: "Kaynak bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        return Ok(resource);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<ResourceResponse>> Create(
        [FromForm] CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
            return Problem(detail: "Yüklenecek dosya zorunludur.", statusCode: StatusCodes.Status400BadRequest);

        if (request.File.Length == 0)
            return Problem(detail: "Boş dosya yüklenemez.", statusCode: StatusCodes.Status400BadRequest);

        if (request.File.Length > _maxFileSize)
            return Problem(detail: "Dosya izin verilen boyut sınırını aşıyor.", statusCode: StatusCodes.Status413PayloadTooLarge);

        if (!Enum.IsDefined(request.Type))
            return Problem(detail: "Kaynak türü geçersiz.", statusCode: StatusCodes.Status400BadRequest);

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var contentType = request.File.ContentType.Split(';', 2)[0].Trim();
        if (!AllowedContentTypes.TryGetValue(extension, out var expectedContentType) ||
            !string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                detail: "Dosya uzantısı veya içerik türü desteklenmiyor.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var originalFileName = Path.GetFileName(request.File.FileName);
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Length > 260)
            return Problem(detail: "Dosya adı geçersiz.", statusCode: StatusCodes.Status400BadRequest);

        await using (var stream = request.File.OpenReadStream())
        {
            if (!await HasValidSignatureAsync(stream, extension, cancellationToken))
            {
                return Problem(
                    detail: "Dosya içeriği, belirtilen dosya türüyle eşleşmiyor.",
                    statusCode: StatusCodes.Status415UnsupportedMediaType);
            }
        }

        var courseMatchesUniversity = await _db.Courses.AnyAsync(
            course => course.Id == request.CourseId && course.UniversityId == request.UniversityId,
            cancellationToken);
        if (!courseMatchesUniversity)
        {
            return Problem(
                detail: "Seçilen ders belirtilen üniversiteye ait değil veya bulunamadı.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var storedFileName = string.Empty;
        await using (var stream = request.File.OpenReadStream())
        {
            storedFileName = await _fileStorage.SaveAsync(stream, extension, cancellationToken);
        }

        var resource = new EducationalResource
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            UniversityId = request.UniversityId,
            CourseId = request.CourseId,
            Type = request.Type,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            ContentType = expectedContentType,
            FileSize = request.File.Length,
            UploadedByUserId = User.GetUserId(),
            UploadedAt = DateTimeOffset.UtcNow
        };

        try
        {
            _db.EducationalResources.Add(resource);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _fileStorage.DeleteAsync(storedFileName, CancellationToken.None);
            throw;
        }

        var resourceDetails = await _db.Courses
            .Where(course => course.Id == resource.CourseId)
            .Select(course => new
            {
                UniversityName = course.University!.Name,
                CourseName = course.Name,
                course.Code
            })
            .SingleAsync(cancellationToken);
        var response = new ResourceResponse(
            resource.Id,
            resource.Title,
            resource.Description,
            new ResourceUniversityResponse(resource.UniversityId, resourceDetails.UniversityName),
            new ResourceCourseResponse(resource.CourseId, resourceDetails.CourseName, resourceDetails.Code),
            resource.Type,
            resource.OriginalFileName,
            resource.FileSize,
            new ResourceUploaderResponse(resource.UploadedByUserId, User.Identity?.Name ?? string.Empty),
            resource.UploadedAt);

        return Created($"/api/resources/{resource.Id}", response);
    }

    [HttpGet("{id:int}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var resource = await _db.EducationalResources
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (resource is null)
            return Problem(detail: "Kaynak bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        FileStream fileStream;
        try
        {
            fileStream = _fileStorage.OpenRead(resource.StoredFileName);
        }
        catch (FileNotFoundException)
        {
            return Problem(detail: "Kaynak dosyası bulunamadı.", statusCode: StatusCodes.Status404NotFound);
        }
        catch (DirectoryNotFoundException)
        {
            return Problem(detail: "Kaynak dosyası bulunamadı.", statusCode: StatusCodes.Status404NotFound);
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(fileStream, resource.ContentType, resource.OriginalFileName);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        UpdateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await _db.EducationalResources
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (resource is null)
            return Problem(detail: "Kaynak bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        if (!CanManage(resource))
            return Problem(detail: "Bu kaynağı güncelleme yetkiniz yok.", statusCode: StatusCodes.Status403Forbidden);

        if (!Enum.IsDefined(request.Type))
            return Problem(detail: "Kaynak türü geçersiz.", statusCode: StatusCodes.Status400BadRequest);

        resource.Title = request.Title.Trim();
        resource.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        resource.Type = request.Type;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var resource = await _db.EducationalResources
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (resource is null)
            return Problem(detail: "Kaynak bulunamadı.", statusCode: StatusCodes.Status404NotFound);

        if (!CanManage(resource))
            return Problem(detail: "Bu kaynağı silme yetkiniz yok.", statusCode: StatusCodes.Status403Forbidden);

        _db.EducationalResources.Remove(resource);
        await _db.SaveChangesAsync(cancellationToken);
        await _fileStorage.DeleteAsync(resource.StoredFileName, cancellationToken);

        return NoContent();
    }

    private bool CanManage(EducationalResource resource) =>
        User.IsInRole("Admin") || resource.UploadedByUserId == User.GetUserId();

    internal static async Task<PagedResponse<ResourceResponse>> ToPagedResponseAsync(
        IQueryable<EducationalResource> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;
        var items = await query
            .OrderByDescending(resource => resource.UploadedAt)
            .Skip(skip > int.MaxValue ? int.MaxValue : (int)skip)
            .Take(pageSize)
            .Select(ResourceProjection())
            .ToListAsync(cancellationToken);

        return new PagedResponse<ResourceResponse>(items, page, pageSize, totalCount);
    }

    internal static Expression<Func<EducationalResource, ResourceResponse>> ResourceProjection() =>
        resource => new ResourceResponse(
            resource.Id,
            resource.Title,
            resource.Description,
            new ResourceUniversityResponse(resource.UniversityId, resource.University!.Name),
            new ResourceCourseResponse(resource.CourseId, resource.Course!.Name, resource.Course.Code),
            resource.Type,
            resource.OriginalFileName,
            resource.FileSize,
            new ResourceUploaderResponse(resource.UploadedByUserId, resource.UploadedByUser!.Username),
            resource.UploadedAt);

    private static async Task<bool> HasValidSignatureAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken)
    {
        var signature = extension switch
        {
            ".pdf" => "%PDF-"u8.ToArray(),
            ".png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            ".jpg" or ".jpeg" => [0xFF, 0xD8, 0xFF],
            ".docx" or ".pptx" => [0x50, 0x4B, 0x03, 0x04],
            _ => []
        };

        if (signature.Length == 0)
            return true;

        var buffer = new byte[signature.Length];
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (bytesRead == 0)
                return false;

            totalRead += bytesRead;
        }

        return buffer.AsSpan().SequenceEqual(signature);
    }
}
