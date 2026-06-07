using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
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

    public async Task<StoredFileSearchResult> SearchAsync(
        StoredFileSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.StoredFiles
            .AsNoTracking()
            .Where(storedFile => storedFile.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            query = query.Where(storedFile => storedFile.OriginalName.Contains(criteria.Name));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContentType))
        {
            query = query.Where(storedFile => storedFile.ContentType == criteria.ContentType);
        }

        if (criteria.CreatedFromUtc.HasValue)
        {
            query = query.Where(storedFile => storedFile.CreatedAtUtc >= criteria.CreatedFromUtc.Value);
        }

        if (criteria.CreatedToUtc.HasValue)
        {
            query = query.Where(storedFile => storedFile.CreatedAtUtc <= criteria.CreatedToUtc.Value);
        }

        var files = await query
            .OrderByDescending(storedFile => storedFile.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(criteria.Tag))
        {
            files = files
                .Where(storedFile => storedFile.Tags.Contains(criteria.Tag, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = files.Count;
        var skip = (criteria.PageNumber - 1) * criteria.PageSize;
        var items = files
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToArray();

        return new StoredFileSearchResult(items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
