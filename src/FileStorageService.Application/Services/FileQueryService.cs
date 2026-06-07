using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Services;

public sealed class FileQueryService : IFileQueryService
{
    private const int MaxPageSize = 100;

    private readonly IStoredFileRepository _storedFileRepository;

    public FileQueryService(IStoredFileRepository storedFileRepository)
    {
        _storedFileRepository = storedFileRepository;
    }

    public async Task<PagedResponse<StoredFileResponse>> SearchAsync(
        FileSearchRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = NormalizeRequest(request);

        var result = await _storedFileRepository.SearchAsync(
            new StoredFileSearchCriteria(
                normalizedRequest.PageNumber,
                normalizedRequest.PageSize,
                normalizedRequest.Name,
                normalizedRequest.Tag,
                normalizedRequest.ContentType,
                normalizedRequest.CreatedFromUtc,
                normalizedRequest.CreatedToUtc),
            cancellationToken);

        return new PagedResponse<StoredFileResponse>(
            result.Items.Select(ToResponse).ToArray(),
            normalizedRequest.PageNumber,
            normalizedRequest.PageSize,
            result.TotalCount);
    }

    private static FileSearchRequest NormalizeRequest(FileSearchRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        return request with
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Name = CleanOptionalText(request.Name),
            Tag = CleanOptionalText(request.Tag),
            ContentType = CleanOptionalText(request.ContentType)
        };
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
