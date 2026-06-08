using FileStorageService.Api.Configuration;
using FileStorageService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureApiLogging();
builder.Services.AddApiServiceConfiguration(builder.Configuration);

var app = builder.Build();

app.UseApiMiddlewareConfiguration();

await app.ApplyDatabaseMigrationsIfEnabledAsync();

app.Run();
