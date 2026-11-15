using System.Security.Claims;
using CheckPoint.Api.Data;
using CheckPoint.Api.Extensions;
using CheckPoint.Api.Middleware;
using CheckPoint.Api.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CheckPoint.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT yapılandırması geçersiz. En az 32 karakterlik Jwt:Key değerini user-secrets ile ayarlayın.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtExpiryMinutes = builder.Configuration.GetValue<int>("Jwt:ExpiryMinutes");
var maxFileSizeMb = builder.Configuration.GetValue<int>("FileStorage:MaxFileSizeMB");
if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    jwtExpiryMinutes <= 0)
{
    throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience ve pozitif Jwt:ExpiryMinutes ayarları gereklidir.");
}
if (maxFileSizeMb <= 0)
{
    throw new InvalidOperationException("FileStorage:MaxFileSizeMB pozitif bir sayı olmalıdır.");
}

var maxRequestBodySize = builder.Configuration.GetValue<long?>("Kestrel:Limits:MaxRequestBodySize") ?? 1024 * 1024;
if (maxRequestBodySize <= 0)
{
    throw new InvalidOperationException("Kestrel:Limits:MaxRequestBodySize pozitif bir sayı olmalıdır.");
}
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxRequestBodySize);

builder.Services
    .AddControllers(options =>
    {
        options.ModelBindingMessageProvider.UseTurkishModelBindingMessages();
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.AllowInputFormatterExceptionMessages = false;
    })
    .ConfigureApiBehaviorOptions(options => options.UseTurkishProblemResponses());
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context => context.UseTurkishTitles());
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddSchemaTransformer<ResourceTypeSchemaTransformer>();
    options.AddSchemaTransformer<ExampleSchemaTransformer>();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<IFileScanner, NoOpFileScanner>();
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddConfiguredCors(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (principal is null ||
                    !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                {
                    context.Fail("The token does not contain a valid user identifier.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var role = await db.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => user.Role)
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                if (role is null || !principal.IsInRole(role))
                    context.Fail("The token user no longer exists or their role has changed.");
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Kimlik doğrulaması gerekli",
                    Detail = "Bu isteğe erişmek için giriş yapmalısınız.",
                    Instance = context.Request.Path
                };
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(problem),
                    context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Erişim reddedildi",
                    Detail = "Bu işlem için yetkiniz yok.",
                    Instance = context.Request.Path
                };
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(problem),
                    context.HttpContext.RequestAborted);
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsProduction())
    app.UseHsts();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CheckPoint API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAdminAsync(db, app.Configuration);
}

app.Run();

public partial class Program;
