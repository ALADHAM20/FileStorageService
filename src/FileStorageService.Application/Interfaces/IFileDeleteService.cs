namespace FileStorageService.Application.Interfaces;

public interface IFileDeleteService
{
    Task<bool> SoftDeleteAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> HardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
