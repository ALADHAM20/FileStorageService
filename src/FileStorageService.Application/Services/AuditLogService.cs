using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Services;

public sealed class AuditLogService : IAuditLogService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task CreateAsync(
        CreateAuditLogRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var auditLog = AuditLog.Create(
            Guid.NewGuid(),
            request.FileId,
            request.Action,
            request.UserId,
            DateTime.UtcNow,
            request.IpAddress,
            request.CorrelationId,
            request.Details);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<AuditLogResponse>> SearchAsync(
        AuditLogSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var pageNumber = request.PageNumber <= 0 ? DefaultPageNumber : request.PageNumber;
        var pageSize = request.PageSize <= 0
            ? DefaultPageSize
            : Math.Min(request.PageSize, MaxPageSize);

        var criteria = new AuditLogSearchCriteria(
            pageNumber,
            pageSize,
            request.FileId,
            request.Action,
            request.UserId,
            request.FromUtc,
            request.ToUtc);

        var result = await _auditLogRepository.SearchAsync(criteria, cancellationToken);
        var items = result.Items.Select(ToResponse).ToArray();

        return new PagedResponse<AuditLogResponse>(
            items,
            pageNumber,
            pageSize,
            result.TotalCount);
    }

    private static AuditLogResponse ToResponse(AuditLog auditLog)
    {
        return new AuditLogResponse(
            auditLog.Id,
            auditLog.FileId,
            auditLog.Action.ToString(),
            auditLog.UserId,
            auditLog.OccurredAtUtc,
            auditLog.IpAddress,
            auditLog.CorrelationId,
            auditLog.Details);
    }
}
