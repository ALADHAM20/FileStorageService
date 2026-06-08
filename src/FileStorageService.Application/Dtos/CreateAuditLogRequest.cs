using FileStorageService.Domain.Enums;

namespace FileStorageService.Application.Dtos;

public sealed record CreateAuditLogRequest(
    Guid? FileId,
    AuditAction Action,
    string UserId,
    string? IpAddress,
    string? CorrelationId,
    string? Details);
