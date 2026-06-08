using System.Security.Claims;
using FileStorageService.Api.Middleware;
using Serilog;

namespace FileStorageService.Api.Configuration;

public static class MiddlewareConfiguration
{
    public static WebApplication UseApiMiddlewareConfiguration(this WebApplication app)
    {
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
                diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            };
        });
        app.UseExceptionHandler();
        app.UseCors(ServiceInjectionConfiguration.GetCorsPolicyName());
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
