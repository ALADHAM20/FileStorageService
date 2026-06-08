using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Options;
using FileStorageService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorageService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileQueryOptions>(configuration.GetSection(FileQueryOptions.SectionName));
        services.Configure<FilePreviewOptions>(configuration.GetSection(FilePreviewOptions.SectionName));
        services.Configure<ResumableUploadOptions>(configuration.GetSection(ResumableUploadOptions.SectionName));
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IFileDeleteService, FileDeleteService>();
        services.AddScoped<IFileDownloadService, FileDownloadService>();
        services.AddScoped<IFilePreviewService, FilePreviewService>();
        services.AddScoped<IFileQueryService, FileQueryService>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IResumableUploadService, ResumableUploadService>();

        return services;
    }
}
