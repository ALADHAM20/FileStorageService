using FileStorageService.Application;
using FileStorageService.Api.Endpoints;
using FileStorageService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

var storageRootPath = builder.Configuration["Storage:RootPath"] ?? "_storage";

builder.Services.AddInfrastructure(connectionString, storageRootPath);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new { Service = "FileStorageService.Api", Status = "Ready" }))
    .WithName("GetApiStatus")
    .WithOpenApi();

app.MapFileEndpoints();

app.Run();
