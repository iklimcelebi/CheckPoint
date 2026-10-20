using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace CheckPoint.Api.OpenApi;

public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token"
        };

        var bearerReference = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        foreach (var path in document.Paths)
        {
            if (string.Equals(path.Key, "/api/resources", StringComparison.OrdinalIgnoreCase) &&
                path.Value.Operations.TryGetValue(OperationType.Post, out var uploadOperation) &&
                uploadOperation.RequestBody?.Content.TryGetValue("multipart/form-data", out var formContent) == true)
            {
                var formSchema = formContent.Schema;
                if (formSchema.Properties.TryGetValue("File", out _))
                {
                    formSchema.Properties["File"] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    };
                }

                if (formSchema.Properties.TryGetValue("Type", out var resourceTypeSchema))
                {
                    resourceTypeSchema.Type = "string";
                    resourceTypeSchema.Format = null;
                    resourceTypeSchema.Enum =
                    [
                        new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.LectureNote)),
                        new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.Presentation)),
                        new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.StudyQuestion)),
                        new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.PastExamQuestion))
                    ];
                    resourceTypeSchema.MaxLength = null;
                    resourceTypeSchema.MinLength = null;
                }
            }

            if (path.Key.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var operation in path.Value.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [bearerReference] = []
                });
            }
        }

        return Task.CompletedTask;
    }
}
