using FileStorageService.Application.Interfaces;
using FileStorageService.Domain.Enums;

namespace FileStorageService.Api.Services;

public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly IAuditLogService _auditLogService;
    private readonly IAuditRequestFactory _auditRequestFactory;

    public AuditLogWriter(
        IAuditLogService auditLogService,
        IAuditRequestFactory auditRequestFactory)
    {
        _auditLogService = auditLogService;
        _auditRequestFactory = auditRequestFactory;
    }

    public async Task WriteAsync(
        Guid? fileId,
        AuditAction action,
        string details,
        CancellationToken cancellationToken)
    {
        var request = _auditRequestFactory.Create(fileId, action, details);

        await _auditLogService.CreateAsync(request, cancellationToken);
    }
}
