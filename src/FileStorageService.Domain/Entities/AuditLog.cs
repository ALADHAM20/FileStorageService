using FileStorageService.Domain.Enums;
using FileStorageService.Domain.Helpers;

namespace FileStorageService.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
        UserId = string.Empty;
        IpAddress = string.Empty;
        CorrelationId = string.Empty;
        Details = string.Empty;
    }

    private AuditLog(
        Guid id,
        Guid? fileId,
        AuditAction action,
        string userId,
        DateTime occurredAtUtc,
        string ipAddress,
        string correlationId,
        string? details)
    {
        Id = id;
        FileId = fileId;
        Action = action;
        UserId = userId;
        OccurredAtUtc = occurredAtUtc;
        IpAddress = ipAddress;
        CorrelationId = correlationId;
        Details = details ?? string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid? FileId { get; private set; }

    public AuditAction Action { get; private set; }

    public string UserId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string IpAddress { get; private set; }

    public string CorrelationId { get; private set; }

    public string Details { get; private set; }

    public static AuditLog Create(
        Guid id,
        Guid? fileId,
        AuditAction action,
        string userId,
        DateTime occurredAtUtc,
        string? ipAddress,
        string? correlationId,
        string? details)
    {
        AuditLogHelper.ValidateCreateRequest(id, action, userId, occurredAtUtc);

        return new AuditLog(
            id,
            fileId,
            action,
            AuditLogHelper.CleanText(userId),
            occurredAtUtc,
            AuditLogHelper.CleanOptionalText(ipAddress),
            AuditLogHelper.CleanOptionalText(correlationId),
            AuditLogHelper.CleanOptionalText(details));
    }
}
