using FileStorageService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Api.Extensions;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsIfEnabledAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            return;
        }

        var retryCount = Math.Max(app.Configuration.GetValue("Database:MigrationRetryCount", 10), 1);
        var retryDelaySeconds = Math.Max(app.Configuration.GetValue("Database:MigrationRetryDelaySeconds", 5), 1);
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");

                return;
            }
            catch (Exception exception) when (attempt < retryCount)
            {
                logger.LogWarning(
                    exception,
                    "Database migration attempt {Attempt} of {RetryCount} failed. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    retryCount,
                    retryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            }
        }
    }
}
