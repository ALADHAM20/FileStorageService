namespace FileStorageService.Api.Requests;

public sealed record CreateResumableUploadSessionRequest(
    string OriginalName,
    string ContentType,
    long TotalSizeBytes,
    IReadOnlyCollection<string>? Tags);
