using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;

namespace FileStorageService.Application.Services;

public sealed class FilePreviewService : IFilePreviewService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IStoredFileRepository _storedFileRepository;

    public FilePreviewService(
        IFileStorageService fileStorageService,
        IStoredFileRepository storedFileRepository)
    {
        _fileStorageService = fileStorageService;
        _storedFileRepository = storedFileRepository;
    }

    public async Task<FilePreviewResponse?> GetPreviewAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("File id cannot be empty.", nameof(id));
        }

        var storedFile = await _storedFileRepository.GetByIdAsync(id, cancellationToken);

        if (storedFile is null || storedFile.IsDeleted || !CanPreview(storedFile.ContentType))
        {
            return null;
        }

        var content = await _fileStorageService.OpenReadAsync(
            storedFile.StoredKey,
            cancellationToken);

        return new FilePreviewResponse(
            content,
            storedFile.ContentType,
            storedFile.SizeBytes);
    }

    private static bool CanPreview(string contentType)
    {
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
