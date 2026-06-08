using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Services;
using FileStorageService.Domain.Entities;
using FileStorageService.Domain.Enums;

namespace FileStorageService.UnitTests;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_SavesAuditLog()
    {
        var repository = new FakeAuditLogRepository();
        var service = new AuditLogService(repository);
        var fileId = Guid.NewGuid();

        await service.CreateAsync(
            new CreateAuditLogRequest(
                fileId,
                AuditAction.Upload,
                "user-1",
                "127.0.0.1",
                "correlation-1",
                "Uploaded file."),
            CancellationToken.None);

        Assert.NotNull(repository.AddedAuditLog);
        Assert.Equal(fileId, repository.AddedAuditLog.FileId);
        Assert.Equal(AuditAction.Upload, repository.AddedAuditLog.Action);
        Assert.Equal("user-1", repository.AddedAuditLog.UserId);
        Assert.True(repository.SaveChangesWasCalled);
    }

    [Fact]
    public async Task SearchAsync_WhenLogsExist_ReturnsPagedResponse()
    {
        var auditLog = AuditLog.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AuditAction.Download,
            "user-1",
            DateTime.UtcNow,
            "127.0.0.1",
            "correlation-1",
            "Downloaded file.");
        var repository = new FakeAuditLogRepository([auditLog]);
        var service = new AuditLogService(repository);

        var response = await service.SearchAsync(
            new AuditLogSearchRequest(
                1,
                20,
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.Single(response.Items);
        Assert.Equal(1, response.PageNumber);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal("Download", response.Items.First().Action);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        private readonly IReadOnlyCollection<AuditLog> _auditLogs;

        public FakeAuditLogRepository(IReadOnlyCollection<AuditLog>? auditLogs = null)
        {
            _auditLogs = auditLogs ?? [];
        }

        public AuditLog? AddedAuditLog { get; private set; }

        public bool SaveChangesWasCalled { get; private set; }

        public Task AddAsync(
            AuditLog auditLog,
            CancellationToken cancellationToken)
        {
            AddedAuditLog = auditLog;

            return Task.CompletedTask;
        }

        public Task<AuditLogSearchResult> SearchAsync(
            AuditLogSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new AuditLogSearchResult(
                _auditLogs,
                _auditLogs.Count));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesWasCalled = true;

            return Task.CompletedTask;
        }
    }
}
