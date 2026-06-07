using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Options;
using FileStorageService.Application.Services;
using FileStorageService.Domain.Entities;
using Microsoft.Extensions.Options;

namespace FileStorageService.UnitTests;

public sealed class FileQueryServiceTests
{
    [Fact]
    public async Task SearchAsync_WhenPagingIsOutOfRange_NormalizesPaging()
    {
        var repository = new FakeStoredFileRepository();
        var service = new FileQueryService(
            repository,
            Options.Create(new FileQueryOptions
            {
                DefaultPageSize = 20,
                MaxPageSize = 100
            }));

        await service.SearchAsync(
            new FileSearchRequest(PageNumber: -1, PageSize: 500),
            CancellationToken.None);

        Assert.NotNull(repository.LastCriteria);
        Assert.Equal(1, repository.LastCriteria.PageNumber);
        Assert.Equal(100, repository.LastCriteria.PageSize);
    }

    [Fact]
    public async Task SearchAsync_WhenRepositoryReturnsFiles_MapsResponse()
    {
        var storedFile = StoredFile.Create(
            Guid.NewGuid(),
            "report.pdf",
            "2026/06/07/key",
            128,
            "application/pdf",
            "abc123",
            ["finance"],
            new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc),
            "user-1");

        var repository = new FakeStoredFileRepository(
            new StoredFileSearchResult([storedFile], TotalCount: 1));
        var service = new FileQueryService(
            repository,
            Options.Create(new FileQueryOptions
            {
                DefaultPageSize = 20,
                MaxPageSize = 100
            }));

        var response = await service.SearchAsync(
            new FileSearchRequest(),
            CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal("report.pdf", response.Items.First().OriginalName);
    }

    private sealed class FakeStoredFileRepository : IStoredFileRepository
    {
        private readonly StoredFileSearchResult _result;

        public FakeStoredFileRepository()
            : this(new StoredFileSearchResult([], 0))
        {
        }

        public FakeStoredFileRepository(StoredFileSearchResult result)
        {
            _result = result;
        }

        public StoredFileSearchCriteria? LastCriteria { get; private set; }

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
            throw new NotImplementedException();
        }

        public Task<StoredFileSearchResult> SearchAsync(
            StoredFileSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            LastCriteria = criteria;

            return Task.FromResult(_result);
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
