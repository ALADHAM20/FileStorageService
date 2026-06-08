using FileStorageService.Domain.Enums;

namespace FileStorageService.Application.Models;

public sealed record AuditLogSearchCriteria(
    int PageNumber,
    int PageSize,
    Guid? FileId,
    AuditAction? Action,
    string? UserId,
    DateTime? FromUtc,
    DateTime? ToUtc);
