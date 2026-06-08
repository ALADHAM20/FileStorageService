using FileStorageService.Application.Interfaces;

namespace FileStorageService.Infrastructure.Storage;

public sealed class LocalResumableUploadStorageService : IResumableUploadStorageService
{
    private const string ResumableFolderName = "_resumable";

    private readonly LocalFileStorageOptions _options;

    public LocalResumableUploadStorageService(LocalFileStorageOptions options)
    {
        _options = options;
    }

    public async Task CreateAsync(
        string tempStoredKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tempPath = BuildTempPath(tempStoredKey);
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        await using var _ = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
    }

    public async Task<long> AppendAsync(
        string tempStoredKey,
        Stream content,
        long uploadOffset,
        CancellationToken cancellationToken)
    {
        if (content is null || !content.CanRead)
        {
            throw new ArgumentException("Chunk content must be readable.", nameof(content));
        }

        var tempPath = BuildTempPath(tempStoredKey);

        await using var output = new FileStream(
            tempPath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        if (output.Length != uploadOffset)
        {
            throw new InvalidOperationException($"Upload offset mismatch. Expected {output.Length}.");
        }

        output.Position = output.Length;
        await content.CopyToAsync(output, cancellationToken);

        return output.Length;
    }

    public Task<Stream> OpenReadAsync(
        string tempStoredKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tempPath = BuildTempPath(tempStoredKey);

        if (!File.Exists(tempPath))
        {
            throw new FileNotFoundException("Temporary upload content was not found.", tempPath);
        }

        Stream stream = new FileStream(
            tempPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string tempStoredKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tempPath = BuildTempPath(tempStoredKey);

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        return Task.CompletedTask;
    }

    private string BuildTempPath(string tempStoredKey)
    {
        if (string.IsNullOrWhiteSpace(tempStoredKey))
        {
            throw new ArgumentException("Temporary stored key is required.", nameof(tempStoredKey));
        }

        if (Path.IsPathRooted(tempStoredKey) || tempStoredKey.Contains('/') || tempStoredKey.Contains('\\'))
        {
            throw new ArgumentException("Temporary stored key must be a file name only.", nameof(tempStoredKey));
        }

        var path = Path.Combine(GetSafeRootPath(), ResumableFolderName, tempStoredKey);

        return EnsurePathIsInsideRoot(path);
    }

    private string GetSafeRootPath()
    {
        return Path.GetFullPath(_options.RootPath);
    }

    private string EnsurePathIsInsideRoot(string path)
    {
        var rootPath = GetSafeRootPath();
        var fullPath = Path.GetFullPath(path);

        if (!fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage path must stay inside the configured root path.");
        }

        return fullPath;
    }
}
