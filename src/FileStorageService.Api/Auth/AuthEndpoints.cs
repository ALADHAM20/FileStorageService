using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FileStorageService.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/mock-token", CreateMockToken)
            .WithName("CreateMockToken")
            .WithOpenApi()
            .AllowAnonymous();

        return app;
    }

    private static IResult CreateMockToken(
        MockTokenRequest request,
        IOptions<JwtOptions> jwtOptions)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.BadRequest("UserId is required.");
        }

        if (!IsSupportedRole(request.Role))
        {
            return Results.BadRequest("Role must be either 'user' or 'admin'.");
        }

        var options = jwtOptions.Value;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserId.Trim()),
            new Claim(ClaimTypes.NameIdentifier, request.UserId.Trim()),
            new Claim(ClaimTypes.Role, request.Role.Trim().ToLowerInvariant())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(8),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(new MockTokenResponse(accessToken));
    }

    private static bool IsSupportedRole(string role)
    {
        return role.Equals("user", StringComparison.OrdinalIgnoreCase)
            || role.Equals("admin", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record MockTokenRequest(string UserId, string Role);

public sealed record MockTokenResponse(string AccessToken);
