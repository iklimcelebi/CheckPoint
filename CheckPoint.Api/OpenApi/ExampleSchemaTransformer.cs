using System.Text.Json.Nodes;
using CheckPoint.Api.Dtos;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CheckPoint.Api.OpenApi;

public sealed class ExampleSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly Dictionary<Type, Func<JsonNode>> Examples = new()
    {
        [typeof(RegisterRequest)] = () => new JsonObject
        {
            ["username"] = "ayse",
            ["email"] = "ayse@example.com",
            ["password"] = "<8-64 karakter, en az bir harf ve bir rakam>"
        },
        [typeof(LoginRequest)] = () => new JsonObject
        {
            ["email"] = "ayse@example.com",
            ["password"] = "<parola>"
        },
        [typeof(UniversityRequest)] = () => new JsonObject
        {
            ["name"] = "Pamukkale Üniversitesi",
            ["city"] = "Denizli"
        },
        [typeof(CourseRequest)] = () => new JsonObject
        {
            ["name"] = "Veri Yapıları",
            ["code"] = "BLG202",
            ["universityId"] = 1
        },
        [typeof(CourseUpdateRequest)] = () => new JsonObject
        {
            ["name"] = "Veri Yapıları ve Algoritmalar",
            ["code"] = "BLG202"
        },
        [typeof(UpdateResourceRequest)] = () => new JsonObject
        {
            ["title"] = "Vize öncesi ders notları",
            ["description"] = "1-7. hafta konuları",
            ["type"] = "LectureNote"
        }
    };

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (Examples.TryGetValue(context.JsonTypeInfo.Type, out var example))
            schema.Examples = [example()];

        return Task.CompletedTask;
    }
}
