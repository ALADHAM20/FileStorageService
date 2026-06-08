using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Domain.Entities;
using FileStorageService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly FileStorageDbContext _dbContext;

    public AuditLogRepository(FileStorageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<AuditLogSearchResult> SearchAsync(
        AuditLogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs.AsNoTracking();

        if (criteria.FileId.HasValue)
        {
            query = query.Where(auditLog => auditLog.FileId == criteria.FileId.Value);
        }

        if (criteria.Action.HasValue)
        {
            query = query.Where(auditLog => auditLog.Action == criteria.Action.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.UserId))
        {
            query = query.Where(auditLog => auditLog.UserId.Contains(criteria.UserId));
        }

        if (criteria.FromUtc.HasValue)
        {
            query = query.Where(auditLog => auditLog.OccurredAtUtc >= criteria.FromUtc.Value);
        }

        if (criteria.ToUtc.HasValue)
        {
            query = query.Where(auditLog => auditLog.OccurredAtUtc <= criteria.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (criteria.PageNumber - 1) * criteria.PageSize;
        var items = await query
            .OrderByDescending(auditLog => auditLog.OccurredAtUtc)
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToArrayAsync(cancellationToken);

        return new AuditLogSearchResult(items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
