namespace FileStorageService.Application.Interfaces;

public interface IResumableUploadStorageService
{
    Task CreateAsync(
        string tempStoredKey,
        CancellationToken cancellationToken);

    Task<long> AppendAsync(
        string tempStoredKey,
        Stream content,
        long uploadOffset,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        string tempStoredKey,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string tempStoredKey,
        CancellationToken cancellationToken);
}
