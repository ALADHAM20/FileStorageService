using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Services;
using FileStorageService.Domain.Entities;

namespace FileStorageService.UnitTests;

public sealed class FileDeleteServiceTests
{
    [Fact]
    public async Task SoftDeleteAsync_WhenFileExists_MarksFileAsDeleted()
    {
        var storedFile = CreateStoredFile();
        var repository = new FakeStoredFileRepository(storedFile);
        var service = new FileDeleteService(new FakeFileStorageService(), repository);

        var deleted = await service.SoftDeleteAsync(storedFile.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.True(storedFile.IsDeleted);
        Assert.True(repository.SaveChangesWasCalled);
    }

    [Fact]
    public async Task HardDeleteAsync_WhenFileExists_RemovesContentAndMetadata()
    {
        var storedFile = CreateStoredFile();
        var storageService = new FakeFileStorageService();
        var repository = new FakeStoredFileRepository(storedFile);
        var service = new FileDeleteService(storageService, repository);

        var deleted = await service.HardDeleteAsync(storedFile.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Equal(storedFile.StoredKey, storageService.DeletedStoredKey);
        Assert.True(repository.RemoveWasCalled);
        Assert.True(repository.SaveChangesWasCalled);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        var service = new FileDeleteService(
            new FakeFileStorageService(),
            new FakeStoredFileRepository(storedFile: null));

        var deleted = await service.SoftDeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(deleted);
    }

    private static StoredFile CreateStoredFile()
    {
        return StoredFile.Create(
            Guid.NewGuid(),
            "report.pdf",
            "2026/06/07/key",
            128,
            "application/pdf",
            "abc123",
            ["finance"],
            new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public string? DeletedStoredKey { get; private set; }

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
            throw new NotImplementedException();
        }

        public Task DeleteAsync(
            string storedKey,
            CancellationToken cancellationToken)
        {
            DeletedStoredKey = storedKey;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeStoredFileRepository : IStoredFileRepository
    {
        private readonly StoredFile? _storedFile;

        public FakeStoredFileRepository(StoredFile? storedFile)
        {
            _storedFile = storedFile;
        }

        public bool RemoveWasCalled { get; private set; }

        public bool SaveChangesWasCalled { get; private set; }

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
            RemoveWasCalled = true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesWasCalled = true;

            return Task.CompletedTask;
        }
    }
}
