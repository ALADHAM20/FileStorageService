using FileStorageService.Application.Dtos;

namespace FileStorageService.Application.Interfaces;

public interface IResumableUploadService
{
    Task<UploadSessionResponse> CreateSessionAsync(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken);

    Task<UploadSessionResponse?> AppendChunkAsync(
        AppendUploadChunkRequest request,
        CancellationToken cancellationToken);

    Task<StoredFileResponse?> CompleteAsync(
        Guid sessionId,
        CancellationToken cancellationToken);
}
