using FileStorageService.Domain.Enums;

namespace FileStorageService.Api.Services;

public interface IAuditLogWriter
{
    Task WriteAsync(
        Guid? fileId,
        AuditAction action,
        string details,
        CancellationToken cancellationToken);
}
