using FileStorageService.Domain.Enums;
using FileStorageService.Domain.Helpers;

namespace FileStorageService.Domain.Entities;

public sealed class UploadSession
{
    private readonly List<string> _tags = [];

    private UploadSession()
    {
        OriginalName = string.Empty;
        ContentType = string.Empty;
        CreatedByUserId = string.Empty;
        TempStoredKey = string.Empty;
    }

    private UploadSession(
        Guid id,
        string originalName,
        string contentType,
        long totalSizeBytes,
        IEnumerable<string> tags,
        string createdByUserId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string tempStoredKey)
    {
        Id = id;
        OriginalName = originalName;
        ContentType = contentType;
        TotalSizeBytes = totalSizeBytes;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = UploadSessionStatus.Active;
        TempStoredKey = tempStoredKey;
        _tags.AddRange(UploadSessionHelper.NormalizeTags(tags));
    }

    public Guid Id { get; private set; }

    public string OriginalName { get; private set; }

    public string ContentType { get; private set; }

    public long TotalSizeBytes { get; private set; }

    public long UploadedBytes { get; private set; }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public string CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public UploadSessionStatus Status { get; private set; }

    public string TempStoredKey { get; private set; }

    public Guid? FinalFileId { get; private set; }

    public static UploadSession Create(
        Guid id,
        string originalName,
        string contentType,
        long totalSizeBytes,
        IEnumerable<string>? tags,
        string createdByUserId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string tempStoredKey)
    {
        UploadSessionHelper.ValidateCreateRequest(
            id,
            originalName,
            contentType,
            totalSizeBytes,
            createdByUserId,
            createdAtUtc,
            expiresAtUtc,
            tempStoredKey);

        return new UploadSession(
            id,
            UploadSessionHelper.CleanText(originalName),
            UploadSessionHelper.CleanText(contentType),
            totalSizeBytes,
            tags ?? [],
            UploadSessionHelper.CleanText(createdByUserId),
            createdAtUtc,
            expiresAtUtc,
            UploadSessionHelper.CleanText(tempStoredKey));
    }

    public void UpdateProgress(long uploadedBytes)
    {
        UploadSessionHelper.ValidateProgress(uploadedBytes, TotalSizeBytes);

        UploadedBytes = uploadedBytes;
    }

    public void Complete(Guid finalFileId, DateTime completedAtUtc)
    {
        UploadSessionHelper.ValidateCompleteRequest(finalFileId, completedAtUtc);

        UploadedBytes = TotalSizeBytes;
        FinalFileId = finalFileId;
        CompletedAtUtc = completedAtUtc;
        Status = UploadSessionStatus.Completed;
    }

    public void Cancel()
    {
        Status = UploadSessionStatus.Cancelled;
    }

    public void Expire()
    {
        Status = UploadSessionStatus.Expired;
    }
}
