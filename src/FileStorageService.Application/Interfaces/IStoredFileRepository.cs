using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Interfaces;

public interface IStoredFileRepository
{
    Task AddAsync(StoredFile storedFile, CancellationToken cancellationToken);

    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
