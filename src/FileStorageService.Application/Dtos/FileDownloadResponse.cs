namespace FileStorageService.Application.Dtos;

public sealed record FileDownloadResponse(
    Stream Content,
    string OriginalName,
    string ContentType,
    long SizeBytes);
