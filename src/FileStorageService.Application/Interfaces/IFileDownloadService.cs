using FileStorageService.Application.Dtos;

namespace FileStorageService.Application.Interfaces;

public interface IFileDownloadService
{
    Task<FileDownloadResponse?> GetDownloadAsync(
        Guid id,
        CancellationToken cancellationToken);
}
