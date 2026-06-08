using FileStorageService.Application.Dtos;
using FileStorageService.Domain.Enums;

namespace FileStorageService.Api.Services;

public sealed class AuditRequestFactory : IAuditRequestFactory
{
    private readonly ICurrentUserService _currentUserService;

    public AuditRequestFactory(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public CreateAuditLogRequest Create(
        Guid? fileId,
        AuditAction action,
        string details)
    {
        return new CreateAuditLogRequest(
            fileId,
            action,
            _currentUserService.UserId,
            _currentUserService.IpAddress,
            _currentUserService.CorrelationId,
            details);
    }
}
