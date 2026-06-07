using FileStorageService.Application.Dtos;

namespace FileStorageService.Application.Interfaces;

public interface IFileUploadService
{
    Task<StoredFileResponse> UploadAsync(
        UploadFileRequest request,
        CancellationToken cancellationToken);
}
