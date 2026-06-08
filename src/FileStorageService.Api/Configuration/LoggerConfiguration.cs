using Serilog;

namespace FileStorageService.Api.Configuration;

public static class LoggerConfiguration
{
    public static ConfigureHostBuilder ConfigureApiLogging(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });

        return host;
    }
}
