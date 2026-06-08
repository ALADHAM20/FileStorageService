using System.Text;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Services;
using FileStorageService.Domain.Entities;

namespace FileStorageService.UnitTests;

public sealed class FileDownloadServiceTests
{
    [Fact]
    public async Task GetDownloadAsync_WhenFileExists_ReturnsContentAndMetadata()
    {
        var storedFile = StoredFile.Create(
            Guid.NewGuid(),
            "report.pdf",
            "2026/06/07/key",
            11,
            "application/pdf",
            "abc123",
            ["finance"],
            new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc),
            "user-1");

        var service = new FileDownloadService(
            new FakeFileStorageService(),
            new FakeStoredFileRepository(storedFile));

        var response = await service.GetDownloadAsync(storedFile.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("report.pdf", response.OriginalName);
        Assert.Equal("application/pdf", response.ContentType);
        Assert.Equal(11, response.SizeBytes);
        Assert.Equal("\"abc123\"", response.ETag);
    }

    [Fact]
    public async Task GetDownloadAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var service = new FileDownloadService(
            new FakeFileStorageService(),
            new FakeStoredFileRepository(storedFile: null));

        var response = await service.GetDownloadAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(response);
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
            Stream content = new MemoryStream(Encoding.UTF8.GetBytes("file stream"));

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

        public void Remove(StoredFile storedFile)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
