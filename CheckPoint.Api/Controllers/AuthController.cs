using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CheckPoint.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(
                user => user.Username == username || user.Email == email,
                cancellationToken))
        {
            return Problem(
                detail: "Bu kullanıcı adı veya e-posta adresi zaten kayıtlı.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(user);
        return CreatedAtAction(nameof(UsersController.Me), "Users", null, response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email == email,
            cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Problem(
                detail: "E-posta veya parola hatalı.",
                statusCode: StatusCodes.Status401Unauthorized);

        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return Ok(new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            ToResponse(user)));
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Username, user.Email, user.Role);
}
