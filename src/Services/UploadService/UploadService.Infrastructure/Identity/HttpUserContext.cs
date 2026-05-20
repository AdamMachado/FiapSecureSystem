using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using UploadService.Application.Abstractions.Identity;

namespace UploadService.Infrastructure.Identity;

public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetRequiredUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId =
            user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var parsedUserId))
            throw new InvalidOperationException("Authenticated user id was not found in the request.");

        return parsedUserId;
    }
}
