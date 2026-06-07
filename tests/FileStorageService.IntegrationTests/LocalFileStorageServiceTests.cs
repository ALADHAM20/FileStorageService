using System.Security.Cryptography;
using System.Text;
using FileStorageService.Infrastructure.Storage;

namespace FileStorageService.IntegrationTests;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        "FileStorageServiceTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_WhenContentIsValid_WritesFileAndReturnsMetadata()
    {
        var service = new LocalFileStorageService(new LocalFileStorageOptions(_rootPath));
        var fileBytes = Encoding.UTF8.GetBytes("hello file storage");
        await using var content = new MemoryStream(fileBytes);
        var createdAtUtc = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);

        var result = await service.SaveAsync(content, createdAtUtc, CancellationToken.None);

        Assert.StartsWith("2026/06/06/", result.StoredKey);
        Assert.Equal(fileBytes.Length, result.SizeBytes);
        Assert.Equal(GetSha256(fileBytes), result.Sha256Checksum);
        Assert.True(File.Exists(BuildContentPath(result.StoredKey)));
    }

    [Fact]
    public async Task OpenReadAsync_WhenFileExists_ReturnsSavedContent()
    {
        var service = new LocalFileStorageService(new LocalFileStorageOptions(_rootPath));
        var fileBytes = Encoding.UTF8.GetBytes("download me");
        await using var content = new MemoryStream(fileBytes);
        var savedFile = await service.SaveAsync(
            content,
            new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        await using var savedContent = await service.OpenReadAsync(
            savedFile.StoredKey,
            CancellationToken.None);

        using var memoryStream = new MemoryStream();
        await savedContent.CopyToAsync(memoryStream);

        Assert.Equal(fileBytes, memoryStream.ToArray());
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_RemovesStoredFolder()
    {
        var service = new LocalFileStorageService(new LocalFileStorageOptions(_rootPath));
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("delete me"));
        var savedFile = await service.SaveAsync(
            content,
            new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        await service.DeleteAsync(savedFile.StoredKey, CancellationToken.None);

        Assert.False(Directory.Exists(BuildStoredDirectoryPath(savedFile.StoredKey)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private string BuildContentPath(string storedKey)
    {
        return Path.Combine(BuildStoredDirectoryPath(storedKey), "content.bin");
    }

    private string BuildStoredDirectoryPath(string storedKey)
    {
        var keyParts = storedKey.Split('/');
        var pathParts = new[] { _rootPath }.Concat(keyParts).ToArray();

        return Path.Combine(pathParts);
    }

    private static string GetSha256(byte[] fileBytes)
    {
        return Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();
    }
}
