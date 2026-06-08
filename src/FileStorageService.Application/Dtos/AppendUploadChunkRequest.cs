namespace FileStorageService.Application.Dtos;

public sealed record AppendUploadChunkRequest(
    Guid SessionId,
    Stream Content,
    long UploadOffset);
