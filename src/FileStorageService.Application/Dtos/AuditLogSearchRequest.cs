using FileStorageService.Domain.Enums;

namespace FileStorageService.Application.Dtos;

public sealed record AuditLogSearchRequest(
    int PageNumber,
    int PageSize,
    Guid? FileId,
    AuditAction? Action,
    string? UserId,
    DateTime? FromUtc,
    DateTime? ToUtc);
