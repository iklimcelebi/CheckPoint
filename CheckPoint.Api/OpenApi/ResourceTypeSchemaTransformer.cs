using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace CheckPoint.Api.OpenApi;

public sealed class ResourceTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(CheckPoint.Api.Models.ResourceType))
        {
            schema.Type = "string";
            schema.Format = null;
            schema.Enum =
            [
                new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.LectureNote)),
                new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.Presentation)),
                new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.StudyQuestion)),
                new OpenApiString(nameof(CheckPoint.Api.Models.ResourceType.PastExamQuestion))
            ];
        }

        return Task.CompletedTask;
    }
}
