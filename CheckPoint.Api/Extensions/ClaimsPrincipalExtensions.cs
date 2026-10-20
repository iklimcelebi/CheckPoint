using System.Security.Claims;

namespace CheckPoint.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var parsedUserId))
            throw new InvalidOperationException("The authenticated principal does not contain a valid user ID.");

        return parsedUserId;
    }
}
