using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorageService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFileQueryService, FileQueryService>();
        services.AddScoped<IFileUploadService, FileUploadService>();

        return services;
    }
}
