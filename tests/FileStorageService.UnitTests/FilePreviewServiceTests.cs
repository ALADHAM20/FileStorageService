using System.Text;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Services;
using FileStorageService.Domain.Entities;

namespace FileStorageService.UnitTests;

public sealed class FilePreviewServiceTests
{
    [Fact]
    public async Task GetPreviewAsync_WhenFileIsPdf_ReturnsPreview()
    {
        var storedFile = CreateStoredFile("application/pdf");
        var service = new FilePreviewService(
            new FakeFileStorageService(),
            new FakeStoredFileRepository(storedFile));

        var response = await service.GetPreviewAsync(storedFile.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("application/pdf", response.ContentType);
    }

    [Fact]
    public async Task GetPreviewAsync_WhenFileIsImage_ReturnsPreview()
    {
        var storedFile = CreateStoredFile("image/png");
        var service = new FilePreviewService(
            new FakeFileStorageService(),
            new FakeStoredFileRepository(storedFile));

        var response = await service.GetPreviewAsync(storedFile.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("image/png", response.ContentType);
    }

    [Fact]
    public async Task GetPreviewAsync_WhenContentTypeIsNotPreviewable_ReturnsNull()
    {
        var storedFile = CreateStoredFile("application/zip");
        var service = new FilePreviewService(
            new FakeFileStorageService(),
            new FakeStoredFileRepository(storedFile));

        var response = await service.GetPreviewAsync(storedFile.Id, CancellationToken.None);

        Assert.Null(response);
    }

    private static StoredFile CreateStoredFile(string contentType)
    {
        return StoredFile.Create(
            Guid.NewGuid(),
            "file",
            "2026/06/07/key",
            11,
            contentType,
            "abc123",
            [],
            new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<StoredFileContentResult> SaveAsync(
            Stream content,
            DateTime createdAtUtc,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenReadAsync(
            string storedKey,
            CancellationToken cancellationToken)
        {
            Stream content = new MemoryStream(Encoding.UTF8.GetBytes("preview stream"));

            return Task.FromResult(content);
        }

        public Task DeleteAsync(
            string storedKey,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeStoredFileRepository : IStoredFileRepository
    {
        private readonly StoredFile? _storedFile;

        public FakeStoredFileRepository(StoredFile? storedFile)
        {
            _storedFile = storedFile;
        }

        public Task AddAsync(
            StoredFile storedFile,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<StoredFile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_storedFile);
        }

        public Task<StoredFileSearchResult> SearchAsync(
            StoredFileSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
