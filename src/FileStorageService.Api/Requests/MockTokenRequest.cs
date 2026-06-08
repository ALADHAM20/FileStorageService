namespace FileStorageService.Api.Requests;

public sealed record MockTokenRequest
{
    /// <summary>User identifier to include in the mock JWT.</summary>
    /// <example>user-1</example>
    public string UserId { get; init; } = string.Empty;

    /// <summary>User role to include in the mock JWT. Supported values are user and admin.</summary>
    /// <example>admin</example>
    public string Role { get; init; } = string.Empty;
}
