using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileStorageService.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FileStorageService.Api.Controllers;

/// <summary>
/// Development authentication helper endpoints.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;

    public AuthController(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// Creates a mock JWT for local Swagger and frontend testing.
    /// </summary>
    [HttpPost("mock-token")]
    [ProducesResponseType(typeof(MockTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<MockTokenResponse> CreateMockToken(
        [FromBody] MockTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest("UserId is required.");
        }

        if (!IsSupportedRole(request.Role))
        {
            return BadRequest("Role must be either 'user' or 'admin'.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserId.Trim()),
            new Claim(ClaimTypes.NameIdentifier, request.UserId.Trim()),
            new Claim(ClaimTypes.Role, request.Role.Trim().ToLowerInvariant())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_jwtOptions.MockTokenLifetimeMinutes),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new MockTokenResponse(accessToken));
    }

    private static bool IsSupportedRole(string role)
    {
        return role.Equals("user", StringComparison.OrdinalIgnoreCase)
            || role.Equals("admin", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record MockTokenRequest(string UserId, string Role);

public sealed record MockTokenResponse(string AccessToken);
