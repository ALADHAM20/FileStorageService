using FileStorageService.Domain.Entities;
using FileStorageService.Application.Models;

namespace FileStorageService.Application.Interfaces;

public interface IStoredFileRepository
{
    Task AddAsync(StoredFile storedFile, CancellationToken cancellationToken);

    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<StoredFileSearchResult> SearchAsync(
        StoredFileSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
