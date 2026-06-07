namespace FileStorageService.Application.Models;

public sealed record StoredFileContentResult(
    string StoredKey,
    long SizeBytes,
    string Sha256Checksum);
