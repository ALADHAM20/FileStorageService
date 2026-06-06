namespace FileStorageService.Domain.Helpers;

public static class StoredFileHelper
{
    public static void ValidateCreateRequest(
        Guid id,
        string originalName,
        string storedKey,
        long sizeBytes,
        string contentType,
        string sha256Checksum,
        DateTime createdAtUtc,
        string createdByUserId,
        int? version)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("File id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(originalName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalName));
        }

        if (string.IsNullOrWhiteSpace(storedKey))
        {
            throw new ArgumentException("Stored key is required.", nameof(storedKey));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "File size must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (string.IsNullOrWhiteSpace(sha256Checksum))
        {
            throw new ArgumentException("SHA-256 checksum is required.", nameof(sha256Checksum));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Created date must be in UTC.", nameof(createdAtUtc));
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        }

        if (version is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than zero when provided.");
        }
    }

    public static string CleanText(string value)
    {
        return value.Trim();
    }

    public static string CleanChecksum(string checksum)
    {
        return checksum.Trim().ToLowerInvariant();
    }

    public static IEnumerable<string> NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
