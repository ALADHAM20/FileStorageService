namespace FileStorageService.Application.Dtos;

public sealed record StoredFileResponse(
    Guid Id,
    string OriginalName,
    string StoredKey,
    long SizeBytes,
    string ContentType,
    string Sha256Checksum,
    IReadOnlyCollection<string> Tags,
    DateTime CreatedAtUtc,
    DateTime? DeletedAtUtc,
    int? Version,
    string CreatedByUserId);
