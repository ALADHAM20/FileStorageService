using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;

namespace FileStorageService.Application.Services;

public sealed class FileDownloadService : IFileDownloadService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IStoredFileRepository _storedFileRepository;

    public FileDownloadService(
        IFileStorageService fileStorageService,
        IStoredFileRepository storedFileRepository)
    {
        _fileStorageService = fileStorageService;
        _storedFileRepository = storedFileRepository;
    }

    public async Task<FileDownloadResponse?> GetDownloadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("File id cannot be empty.", nameof(id));
        }

        var storedFile = await _storedFileRepository.GetByIdAsync(id, cancellationToken);

        if (storedFile is null || storedFile.IsDeleted)
        {
            return null;
        }

        var content = await _fileStorageService.OpenReadAsync(
            storedFile.StoredKey,
            cancellationToken);

        return new FileDownloadResponse(
            content,
            storedFile.OriginalName,
            storedFile.ContentType,
            storedFile.SizeBytes);
    }
}
