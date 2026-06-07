using System.Text;
using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Services;
using FileStorageService.Domain.Entities;

namespace FileStorageService.UnitTests;

public sealed class FileUploadServiceTests
{
    [Fact]
    public async Task UploadAsync_WhenRequestIsValid_SavesContentAndMetadata()
    {
        var storageService = new FakeFileStorageService();
        var repository = new FakeStoredFileRepository();
        var uploadService = new FileUploadService(storageService, repository);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("file content"));

        var response = await uploadService.UploadAsync(
            new UploadFileRequest(
                content,
                "report.pdf",
                "application/pdf",
                ["finance"],
                "user-1"),
            CancellationToken.None);

        Assert.Equal("report.pdf", response.OriginalName);
        Assert.Equal("application/pdf", response.ContentType);
        Assert.Equal("2026/06/07/key", response.StoredKey);
        Assert.Equal("abc123", response.Sha256Checksum);
        Assert.Equal("user-1", response.CreatedByUserId);
        Assert.True(storageService.SaveWasCalled);
        Assert.NotNull(repository.AddedFile);
        Assert.True(repository.SaveChangesWasCalled);
    }

    [Fact]
    public async Task UploadAsync_WhenOriginalNameIsMissing_Throws()
    {
        var uploadService = new FileUploadService(
            new FakeFileStorageService(),
            new FakeStoredFileRepository());

        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("file content"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            uploadService.UploadAsync(
                new UploadFileRequest(
                    content,
                    "",
                    "application/pdf",
                    [],
                    "user-1"),
                CancellationToken.None));

        Assert.Equal("request", exception.ParamName);
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public bool SaveWasCalled { get; private set; }

        public Task<StoredFileContentResult> SaveAsync(
            Stream content,
            DateTime createdAtUtc,
            CancellationToken cancellationToken)
        {
            SaveWasCalled = true;

            return Task.FromResult(new StoredFileContentResult(
                "2026/06/07/key",
                12,
                "abc123"));
        }

        public Task<Stream> OpenReadAsync(
            string storedKey,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
        public StoredFile? AddedFile { get; private set; }

        public bool SaveChangesWasCalled { get; private set; }

        public Task AddAsync(
            StoredFile storedFile,
            CancellationToken cancellationToken)
        {
            AddedFile = storedFile;

            return Task.CompletedTask;
        }

        public Task<StoredFile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredFile?>(null);
        }

        public Task<StoredFileSearchResult> SearchAsync(
            StoredFileSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredFileSearchResult([], 0));
        }

        public void Remove(StoredFile storedFile)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesWasCalled = true;

            return Task.CompletedTask;
        }
    }
}
