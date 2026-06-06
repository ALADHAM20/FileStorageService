using FileStorageService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Infrastructure.DbContext;

public sealed class FileStorageDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileStorageDbContext).Assembly);
    }
}
