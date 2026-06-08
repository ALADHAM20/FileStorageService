using FileStorageService.Application.Models;
using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);

    Task<AuditLogSearchResult> SearchAsync(
        AuditLogSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
