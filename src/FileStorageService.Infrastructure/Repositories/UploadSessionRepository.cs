using FileStorageService.Application.Interfaces;
using FileStorageService.Domain.Entities;
using FileStorageService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Infrastructure.Repositories;

public sealed class UploadSessionRepository : IUploadSessionRepository
{
    private readonly FileStorageDbContext _dbContext;

    public UploadSessionRepository(FileStorageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        UploadSession uploadSession,
        CancellationToken cancellationToken)
    {
        await _dbContext.UploadSessions.AddAsync(uploadSession, cancellationToken);
    }

    public async Task<UploadSession?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _dbContext.UploadSessions
            .FirstOrDefaultAsync(uploadSession => uploadSession.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
