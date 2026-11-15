using System.Text.Json.Nodes;
using CheckPoint.Api.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CheckPoint.Api.OpenApi;

public sealed class ResourceTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (type == typeof(ResourceType))
            ApplyResourceTypeEnum(schema);

        return Task.CompletedTask;
    }

    internal static void ApplyResourceTypeEnum(OpenApiSchema schema)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = null;
        schema.MinLength = null;
        schema.MaxLength = null;
        schema.Pattern = null;
        schema.Enum = Enum.GetNames<ResourceType>().Select(name => (JsonNode)JsonValue.Create(name)).ToList();
    }
}
