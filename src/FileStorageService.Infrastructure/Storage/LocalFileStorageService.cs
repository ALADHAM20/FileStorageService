using System.Security.Cryptography;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;

namespace FileStorageService.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const string ContentFileName = "content.bin";
    private const string TempFolderName = "_temp";

    private readonly LocalFileStorageOptions _options;

    public LocalFileStorageService(LocalFileStorageOptions options)
    {
        _options = options;
    }

    public async Task<StoredFileContentResult> SaveAsync(
        Stream content,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (!content.CanRead)
        {
            throw new ArgumentException("Content stream must be readable.", nameof(content));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Created date must be in UTC.", nameof(createdAtUtc));
        }

        var storedKey = CreateStoredKey(createdAtUtc);
        var finalDirectory = BuildStorageDirectoryPath(storedKey);
        var finalPath = BuildContentPath(storedKey);
        var tempPath = BuildTempPath();

        Directory.CreateDirectory(finalDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        try
        {
            var result = await WriteTempFileAndCalculateChecksumAsync(
                content,
                tempPath,
                cancellationToken);

            if (result.SizeBytes == 0)
            {
                throw new InvalidOperationException("Cannot save an empty file.");
            }

            File.Move(tempPath, finalPath);

            return new StoredFileContentResult(
                storedKey,
                result.SizeBytes,
                result.Sha256Checksum);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public Task<Stream> OpenReadAsync(
        string storedKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contentPath = BuildContentPath(storedKey);

        if (!File.Exists(contentPath))
        {
            throw new FileNotFoundException("Stored file content was not found.", contentPath);
        }

        Stream stream = new FileStream(
            contentPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string storedKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directoryPath = BuildStorageDirectoryPath(storedKey);

        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }

        return Task.CompletedTask;
    }

    private static string CreateStoredKey(DateTime createdAtUtc)
    {
        return string.Join(
            '/',
            createdAtUtc.Year.ToString("0000"),
            createdAtUtc.Month.ToString("00"),
            createdAtUtc.Day.ToString("00"),
            Guid.NewGuid().ToString("N"));
    }

    private async Task<FileWriteResult> WriteTempFileAndCalculateChecksumAsync(
        Stream content,
        string tempPath,
        CancellationToken cancellationToken)
    {
        await using var output = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        using var sha256 = SHA256.Create();
        var buffer = new byte[81920];
        long sizeBytes = 0;

        while (true)
        {
            var bytesRead = await content.ReadAsync(buffer, cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            sizeBytes += bytesRead;
        }

        sha256.TransformFinalBlock([], 0, 0);

        return new FileWriteResult(
            SizeBytes: sizeBytes,
            Sha256Checksum: Convert.ToHexString(sha256.Hash!).ToLowerInvariant());
    }

    private string BuildTempPath()
    {
        var tempDirectory = Path.Combine(GetSafeRootPath(), TempFolderName);
        var tempFileName = $"{Guid.NewGuid():N}.tmp";

        return EnsurePathIsInsideRoot(Path.Combine(tempDirectory, tempFileName));
    }

    private string BuildContentPath(string storedKey)
    {
        var directoryPath = BuildStorageDirectoryPath(storedKey);

        return EnsurePathIsInsideRoot(Path.Combine(directoryPath, ContentFileName));
    }

    private string BuildStorageDirectoryPath(string storedKey)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
        {
            throw new ArgumentException("Stored key is required.", nameof(storedKey));
        }

        if (Path.IsPathRooted(storedKey))
        {
            throw new ArgumentException("Stored key cannot be an absolute path.", nameof(storedKey));
        }

        var keyParts = storedKey.Split(
            ['/', '\\'],
            StringSplitOptions.RemoveEmptyEntries);

        if (keyParts.Any(part => part is "." or ".."))
        {
            throw new ArgumentException("Stored key cannot contain relative path segments.", nameof(storedKey));
        }

        var pathParts = new[] { GetSafeRootPath() }.Concat(keyParts).ToArray();

        return EnsurePathIsInsideRoot(Path.Combine(pathParts));
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

    private sealed record FileWriteResult(long SizeBytes, string Sha256Checksum);
}
