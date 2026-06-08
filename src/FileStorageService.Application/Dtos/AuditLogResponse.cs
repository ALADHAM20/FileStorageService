namespace FileStorageService.Application.Dtos;

public sealed record AuditLogResponse(
    Guid Id,
    Guid? FileId,
    string Action,
    string UserId,
    DateTime OccurredAtUtc,
    string IpAddress,
    string CorrelationId,
    string Details);
