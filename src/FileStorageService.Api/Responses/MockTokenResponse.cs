namespace FileStorageService.Api.Responses;

public sealed record MockTokenResponse
{
    public MockTokenResponse(string accessToken)
    {
        AccessToken = accessToken;
    }

    /// <summary>JWT access token used as a Bearer token for secured endpoints.</summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string AccessToken { get; init; }
}
