using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Options;
using Microsoft.Extensions.Options;

namespace FileStorageService.Application.Services;

public sealed class FilePreviewService : IFilePreviewService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly FilePreviewOptions _options;
    private readonly IStoredFileRepository _storedFileRepository;

    public FilePreviewService(
        IFileStorageService fileStorageService,
        IStoredFileRepository storedFileRepository,
        IOptions<FilePreviewOptions> options)
    {
        _fileStorageService = fileStorageService;
        _storedFileRepository = storedFileRepository;
        _options = options.Value;
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

        if (storedFile is null || storedFile.IsDeleted || !CanPreview(storedFile.ContentType, _options))
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

    private static bool CanPreview(string contentType, FilePreviewOptions options)
    {
        return options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)
            || options.AllowedContentTypePrefixes.Any(prefix =>
                contentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
