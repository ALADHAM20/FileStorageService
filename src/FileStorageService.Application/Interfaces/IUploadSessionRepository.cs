using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Interfaces;

public interface IUploadSessionRepository
{
    Task AddAsync(
        UploadSession uploadSession,
        CancellationToken cancellationToken);

    Task<UploadSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
