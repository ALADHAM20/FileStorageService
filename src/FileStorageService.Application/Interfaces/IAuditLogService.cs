using FileStorageService.Application.Dtos;

namespace FileStorageService.Application.Interfaces;

public interface IAuditLogService
{
    Task CreateAsync(
        CreateAuditLogRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<AuditLogResponse>> SearchAsync(
        AuditLogSearchRequest request,
        CancellationToken cancellationToken);
}
