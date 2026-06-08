using System.Security.Claims;

namespace FileStorageService.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId
    {
        get
        {
            var user = GetHttpContext().User;

            return user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User id claim is missing.");
        }
    }

    public string? IpAddress => GetHttpContext().Connection.RemoteIpAddress?.ToString();

    public string CorrelationId => GetHttpContext().TraceIdentifier;

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available.");
    }
}
