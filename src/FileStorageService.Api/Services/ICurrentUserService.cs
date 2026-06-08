namespace FileStorageService.Api.Services;

public interface ICurrentUserService
{
    string UserId { get; }

    string? IpAddress { get; }

    string CorrelationId { get; }
}
