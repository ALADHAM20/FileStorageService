using FileStorageService.Application.Models;

namespace FileStorageService.Application.Interfaces;

public interface IFileStorageService
{
    Task<StoredFileContentResult> SaveAsync(
        Stream content,
        DateTime createdAtUtc,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        string storedKey,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string storedKey,
        CancellationToken cancellationToken);
}
