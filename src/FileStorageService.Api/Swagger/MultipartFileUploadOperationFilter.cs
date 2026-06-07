using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FileStorageService.Api.Swagger;

public sealed class MultipartFileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!IsFileUploadAction(context))
        {
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Required = new HashSet<string> { "file" },
                        Properties =
                        {
                            ["tags"] = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "Optional comma-separated tags. Send this field before the file field."
                            },
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "File content to upload."
                            }
                        }
                    }
                }
            }
        };
    }

    private static bool IsFileUploadAction(OperationFilterContext context)
    {
        return context.ApiDescription.HttpMethod?.Equals("POST", StringComparison.OrdinalIgnoreCase) == true
            && context.ApiDescription.RelativePath?.Equals("api/files", StringComparison.OrdinalIgnoreCase) == true;
    }
}
