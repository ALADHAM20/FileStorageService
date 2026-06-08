namespace FileStorageService.Domain.Helpers;

public static class UploadSessionHelper
{
    public static void ValidateCreateRequest(
        Guid id,
        string originalName,
        string contentType,
        long totalSizeBytes,
        string createdByUserId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string tempStoredKey)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Upload session id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(originalName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (totalSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSizeBytes), "Total file size must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Created date must be in UTC.", nameof(createdAtUtc));
        }

        if (expiresAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Expiry date must be in UTC.", nameof(expiresAtUtc));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("Expiry date must be after the created date.", nameof(expiresAtUtc));
        }

        if (string.IsNullOrWhiteSpace(tempStoredKey))
        {
            throw new ArgumentException("Temporary stored key is required.", nameof(tempStoredKey));
        }
    }

    public static void ValidateProgress(long uploadedBytes, long totalSizeBytes)
    {
        if (uploadedBytes < 0 || uploadedBytes > totalSizeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(uploadedBytes), "Uploaded bytes must be within the total file size.");
        }
    }

    public static void ValidateCompleteRequest(Guid finalFileId, DateTime completedAtUtc)
    {
        if (finalFileId == Guid.Empty)
        {
            throw new ArgumentException("Final file id cannot be empty.", nameof(finalFileId));
        }

        if (completedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Completed date must be in UTC.", nameof(completedAtUtc));
        }
    }

    public static string CleanText(string value)
    {
        return value.Trim();
    }

    public static IEnumerable<string> NormalizeTags(IEnumerable<string> tags)
    {
        return StoredFileHelper.NormalizeTags(tags);
    }
}
