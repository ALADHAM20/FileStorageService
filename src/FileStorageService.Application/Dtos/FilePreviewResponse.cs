namespace FileStorageService.Application.Dtos;

public sealed record FilePreviewResponse(
    Stream Content,
    string ContentType,
    long SizeBytes,
    string ETag);
