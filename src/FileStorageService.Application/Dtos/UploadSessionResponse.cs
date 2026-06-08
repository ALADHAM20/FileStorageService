namespace FileStorageService.Application.Dtos;

public sealed record UploadSessionResponse(
    Guid Id,
    string OriginalName,
    string ContentType,
    long TotalSizeBytes,
    long UploadedBytes,
    IReadOnlyCollection<string> Tags,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    Guid? FinalFileId);
