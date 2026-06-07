using FileStorageService.Application.Dtos;

namespace FileStorageService.Application.Interfaces;

public interface IFilePreviewService
{
    Task<FilePreviewResponse?> GetPreviewAsync(
        Guid id,
        CancellationToken cancellationToken);
}
