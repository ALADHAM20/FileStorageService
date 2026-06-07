using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Services;

public sealed class FileUploadService : IFileUploadService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IStoredFileRepository _storedFileRepository;

    public FileUploadService(
        IFileStorageService fileStorageService,
        IStoredFileRepository storedFileRepository)
    {
        _fileStorageService = fileStorageService;
        _storedFileRepository = storedFileRepository;
    }

    public async Task<StoredFileResponse> UploadAsync(
        UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var createdAtUtc = DateTime.UtcNow;
        StoredFileContentResult? storedContent = null;

        try
        {
            storedContent = await _fileStorageService.SaveAsync(
                request.Content,
                createdAtUtc,
                cancellationToken);

            var storedFile = StoredFile.Create(
                Guid.NewGuid(),
                request.OriginalName,
                storedContent.StoredKey,
                storedContent.SizeBytes,
                request.ContentType,
                storedContent.Sha256Checksum,
                request.Tags,
                createdAtUtc,
                request.CreatedByUserId);

            await _storedFileRepository.AddAsync(storedFile, cancellationToken);
            await _storedFileRepository.SaveChangesAsync(cancellationToken);

            return ToResponse(storedFile);
        }
        catch
        {
            if (storedContent is not null)
            {
                await _fileStorageService.DeleteAsync(storedContent.StoredKey, CancellationToken.None);
            }

            throw;
        }
    }

    private static void ValidateRequest(UploadFileRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Content is null)
        {
            throw new ArgumentException("File content is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OriginalName))
        {
            throw new ArgumentException("Original file name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ArgumentException("Content type is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.CreatedByUserId))
        {
            throw new ArgumentException("Created by user id is required.", nameof(request));
        }
    }

    private static StoredFileResponse ToResponse(StoredFile storedFile)
    {
        return new StoredFileResponse(
            storedFile.Id,
            storedFile.OriginalName,
            storedFile.StoredKey,
            storedFile.SizeBytes,
            storedFile.ContentType,
            storedFile.Sha256Checksum,
            storedFile.Tags,
            storedFile.CreatedAtUtc,
            storedFile.DeletedAtUtc,
            storedFile.Version,
            storedFile.CreatedByUserId);
    }
}
