using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CheckPoint.Api.OpenApi;

public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token"
        };

        foreach (var (path, pathItem) in document.Paths)
        {
            if (pathItem.Operations is null)
                continue;

            if (string.Equals(path, "/api/resources", StringComparison.OrdinalIgnoreCase) &&
                pathItem.Operations.TryGetValue(HttpMethod.Post, out var uploadOperation))
            {
                FixUploadForm(uploadOperation);
            }

            if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
                });
            }
        }

        return Task.CompletedTask;
    }

    private static void FixUploadForm(OpenApiOperation operation)
    {
        if (operation.RequestBody?.Content is null ||
            !operation.RequestBody.Content.TryGetValue("multipart/form-data", out var formContent) ||
            formContent.Schema is not OpenApiSchema formSchema ||
            formSchema.Properties is null)
        {
            return;
        }

        if (formSchema.Properties.ContainsKey("File"))
            formSchema.Properties["File"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };

        if (formSchema.Properties.TryGetValue("Type", out var typeSchema) && typeSchema is OpenApiSchema resourceTypeSchema)
            ResourceTypeSchemaTransformer.ApplyResourceTypeEnum(resourceTypeSchema);
    }
}
