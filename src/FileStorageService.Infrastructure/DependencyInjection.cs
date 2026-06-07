using FileStorageService.Application.Interfaces;
using FileStorageService.Infrastructure.DbContext;
using FileStorageService.Infrastructure.Repositories;
using FileStorageService.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorageService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string storageRootPath)
    {
        services.AddDbContext<FileStorageDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IStoredFileRepository, StoredFileRepository>();
        services.AddSingleton(new LocalFileStorageOptions(storageRootPath));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
