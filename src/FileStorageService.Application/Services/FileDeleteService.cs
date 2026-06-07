using FileStorageService.Application.Interfaces;

namespace FileStorageService.Application.Services;

public sealed class FileDeleteService : IFileDeleteService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IStoredFileRepository _storedFileRepository;

    public FileDeleteService(
        IFileStorageService fileStorageService,
        IStoredFileRepository storedFileRepository)
    {
        _fileStorageService = fileStorageService;
        _storedFileRepository = storedFileRepository;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("File id cannot be empty.", nameof(id));
        }

        var storedFile = await _storedFileRepository.GetByIdAsync(id, cancellationToken);

        if (storedFile is null)
        {
            return false;
        }

        storedFile.SoftDelete(DateTime.UtcNow);

        await _storedFileRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> HardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("File id cannot be empty.", nameof(id));
        }

        var storedFile = await _storedFileRepository.GetByIdAsync(id, cancellationToken);

        if (storedFile is null)
        {
            return false;
        }

        await _fileStorageService.DeleteAsync(storedFile.StoredKey, cancellationToken);
        _storedFileRepository.Remove(storedFile);
        await _storedFileRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
