using FileStorageService.Application;
using FileStorageService.Api.Auth;
using FileStorageService.Api.Endpoints;
using FileStorageService.Api.ErrorHandling;
using FileStorageService.Api.Middleware;
using FileStorageService.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddJwtSwaggerSecurity());
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
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
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("UserId", httpContext.User.Identity?.Name);
    };
});
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { Service = "FileStorageService.Api", Status = "Ready" }))
    .WithName("GetApiStatus")
    .WithOpenApi();

app.MapAuthEndpoints();
app.MapFileEndpoints();

app.Run();
