using FileStorageService.Application;
using FileStorageService.Api.ErrorHandling;
using FileStorageService.Api.Options;
using FileStorageService.Api.Services;
using FileStorageService.Api.Swagger;
using FileStorageService.Infrastructure;

namespace FileStorageService.Api.Configuration;

public static class ServiceInjectionConfiguration
{
    private const string CorsPolicyName = "FrontendClient";

    public static IServiceCollection AddApiServiceConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddControllers();
        services.AddHttpContextAccessor();
        services.AddApiScopedServices();
        services.AddApiOptions(configuration);
        services.AddApiSwagger();
        services.AddJwtAuthenticationConfiguration(configuration);
        services.AddApiErrorHandling();
        services.AddApplication(configuration);
        services.AddApiCors(configuration);
        services.AddInfrastructureServices(configuration);

        return services;
    }

    public static string GetCorsPolicyName()
    {
        return CorsPolicyName;
    }

    private static IServiceCollection AddApiScopedServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditRequestFactory, AuditRequestFactory>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<MultipartUploadRequestReader>();

        return services;
    }

    private static IServiceCollection AddApiOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UploadOptions>(configuration.GetSection(UploadOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddJwtSwaggerSecurity();
            options.OperationFilter<MultipartFileUploadOperationFilter>();

            var xmlPath = Path.Combine(AppContext.BaseDirectory, "FileStorageService.Api.xml");
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    private static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }

    private static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var storageRootPath = configuration["Storage:RootPath"] ?? "_storage";

        services.AddInfrastructure(connectionString, storageRootPath);

        return services;
    }
}
