using System.Text;
using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Services;
using FileStorageService.Domain.Entities;

namespace FileStorageService.UnitTests;

public sealed class ResumableUploadServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_WhenRequestIsValid_CreatesSession()
    {
        var uploadSessionRepository = new FakeUploadSessionRepository();
        var resumableStorage = new FakeResumableUploadStorageService();
        var service = CreateService(
            uploadSessionRepository: uploadSessionRepository,
            resumableStorage: resumableStorage);

        var response = await service.CreateSessionAsync(
            new CreateUploadSessionRequest(
                "video.mp4",
                "video/mp4",
                1024,
                ["training"],
                "user-1"),
            CancellationToken.None);

        Assert.Equal("video.mp4", response.OriginalName);
        Assert.Equal(1024, response.TotalSizeBytes);
        Assert.Equal(0, response.UploadedBytes);
        Assert.Equal("Active", response.Status);
        Assert.NotNull(uploadSessionRepository.UploadSession);
        Assert.True(resumableStorage.CreateWasCalled);
    }

    [Fact]
    public async Task CompleteAsync_WhenSessionIsFullyUploaded_CreatesStoredFile()
    {
        var uploadSession = UploadSession.Create(
            Guid.NewGuid(),
            "video.mp4",
            "video/mp4",
            12,
            ["training"],
            "user-1",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            "temp.upload");
        uploadSession.UpdateProgress(12);

        var storedFileRepository = new FakeStoredFileRepository();
        var uploadSessionRepository = new FakeUploadSessionRepository(uploadSession);
        var resumableStorage = new FakeResumableUploadStorageService();
        var service = CreateService(
            storedFileRepository,
            uploadSessionRepository,
            resumableStorage);

        var response = await service.CompleteAsync(uploadSession.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("video.mp4", response.OriginalName);
        Assert.Equal("video/mp4", response.ContentType);
        Assert.NotNull(storedFileRepository.StoredFile);
        Assert.Equal(storedFileRepository.StoredFile.Id, uploadSession.FinalFileId);
        Assert.True(resumableStorage.DeleteWasCalled);
    }

    private static ResumableUploadService CreateService(
        FakeStoredFileRepository? storedFileRepository = null,
        FakeUploadSessionRepository? uploadSessionRepository = null,
        FakeResumableUploadStorageService? resumableStorage = null)
    {
        return new ResumableUploadService(
            new FakeFileStorageService(),
            resumableStorage ?? new FakeResumableUploadStorageService(),
            storedFileRepository ?? new FakeStoredFileRepository(),
            uploadSessionRepository ?? new FakeUploadSessionRepository());
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<StoredFileContentResult> SaveAsync(
            Stream content,
            DateTime createdAtUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredFileContentResult(
                "2026/06/08/key",
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
            return Task.CompletedTask;
        }
    }

    private sealed class FakeResumableUploadStorageService : IResumableUploadStorageService
    {
        public bool CreateWasCalled { get; private set; }

        public bool DeleteWasCalled { get; private set; }

        public Task CreateAsync(
            string tempStoredKey,
            CancellationToken cancellationToken)
        {
            CreateWasCalled = true;

            return Task.CompletedTask;
        }

        public Task<long> AppendAsync(
            string tempStoredKey,
            Stream content,
            long uploadOffset,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(uploadOffset + content.Length);
        }

        public Task<Stream> OpenReadAsync(
            string tempStoredKey,
            CancellationToken cancellationToken)
        {
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("file content"));

            return Task.FromResult(stream);
        }

        public Task DeleteAsync(
            string tempStoredKey,
            CancellationToken cancellationToken)
        {
            DeleteWasCalled = true;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeStoredFileRepository : IStoredFileRepository
    {
        public StoredFile? StoredFile { get; private set; }

        public Task AddAsync(
            StoredFile storedFile,
            CancellationToken cancellationToken)
        {
            StoredFile = storedFile;

            return Task.CompletedTask;
        }

        public Task<StoredFile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUploadSessionRepository : IUploadSessionRepository
    {
        public FakeUploadSessionRepository(UploadSession? uploadSession = null)
        {
            UploadSession = uploadSession;
        }

        public UploadSession? UploadSession { get; private set; }

        public Task AddAsync(
            UploadSession uploadSession,
            CancellationToken cancellationToken)
        {
            UploadSession = uploadSession;

            return Task.CompletedTask;
        }

        public Task<UploadSession?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(UploadSession);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
