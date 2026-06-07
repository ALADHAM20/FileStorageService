using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorageService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFileDeleteService, FileDeleteService>();
        services.AddScoped<IFileDownloadService, FileDownloadService>();
        services.AddScoped<IFilePreviewService, FilePreviewService>();
        services.AddScoped<IFileQueryService, FileQueryService>();
        services.AddScoped<IFileUploadService, FileUploadService>();

        return services;
    }
}
