using FileStorageService.Application.Interfaces;
using FileStorageService.Infrastructure.DbContext;
using FileStorageService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorageService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<FileStorageDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IStoredFileRepository, StoredFileRepository>();

        return services;
    }
}
