using FileStorageService.Application.Dtos;

namespace FileStorageService.Application.Interfaces;

public interface IFileQueryService
{
    Task<PagedResponse<StoredFileResponse>> SearchAsync(
        FileSearchRequest request,
        CancellationToken cancellationToken);
}
