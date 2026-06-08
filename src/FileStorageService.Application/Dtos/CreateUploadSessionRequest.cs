namespace FileStorageService.Application.Dtos;

public sealed record CreateUploadSessionRequest(
    string OriginalName,
    string ContentType,
    long TotalSizeBytes,
    IReadOnlyCollection<string> Tags,
    string CreatedByUserId);
