using FileStorageService.Application.Interfaces;
using FileStorageService.Domain.Entities;
using FileStorageService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Infrastructure.Repositories;

public sealed class StoredFileRepository : IStoredFileRepository
{
    private readonly FileStorageDbContext _dbContext;

    public StoredFileRepository(FileStorageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(StoredFile storedFile, CancellationToken cancellationToken)
    {
        await _dbContext.StoredFiles.AddAsync(storedFile, cancellationToken);
    }

    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.StoredFiles
            .FirstOrDefaultAsync(storedFile => storedFile.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
