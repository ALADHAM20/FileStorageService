using FileStorageService.Domain.Enums;

namespace FileStorageService.Domain.Helpers;

public static class AuditLogHelper
{
    public static void ValidateCreateRequest(
        Guid id,
        AuditAction action,
        string userId,
        DateTime occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Audit log id cannot be empty.", nameof(id));
        }

        if (!Enum.IsDefined(action))
        {
            throw new ArgumentException("Audit action is invalid.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Occurred date must be in UTC.", nameof(occurredAtUtc));
        }
    }

    public static string CleanText(string value)
    {
        return value.Trim();
    }

    public static string CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
