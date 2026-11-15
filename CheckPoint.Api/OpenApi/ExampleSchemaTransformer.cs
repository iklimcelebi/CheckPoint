using CheckPoint.Api.Dtos;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace CheckPoint.Api.OpenApi;

public sealed class ExampleSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly Dictionary<Type, Func<OpenApiObject>> Examples = new()
    {
        [typeof(RegisterRequest)] = () => new OpenApiObject
        {
            ["username"] = new OpenApiString("ayse"),
            ["email"] = new OpenApiString("ayse@example.com"),
            ["password"] = new OpenApiString("<8-64 karakter, en az bir harf ve bir rakam>")
        },
        [typeof(LoginRequest)] = () => new OpenApiObject
        {
            ["email"] = new OpenApiString("ayse@example.com"),
            ["password"] = new OpenApiString("<parola>")
        },
        [typeof(UniversityRequest)] = () => new OpenApiObject
        {
            ["name"] = new OpenApiString("Pamukkale Üniversitesi"),
            ["city"] = new OpenApiString("Denizli")
        },
        [typeof(CourseRequest)] = () => new OpenApiObject
        {
            ["name"] = new OpenApiString("Veri Yapıları"),
            ["code"] = new OpenApiString("BLG202"),
            ["universityId"] = new OpenApiInteger(1)
        },
        [typeof(CourseUpdateRequest)] = () => new OpenApiObject
        {
            ["name"] = new OpenApiString("Veri Yapıları ve Algoritmalar"),
            ["code"] = new OpenApiString("BLG202")
        },
        [typeof(UpdateResourceRequest)] = () => new OpenApiObject
        {
            ["title"] = new OpenApiString("Vize öncesi ders notları"),
            ["description"] = new OpenApiString("1-7. hafta konuları"),
            ["type"] = new OpenApiString("LectureNote")
        }
    };

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (Examples.TryGetValue(context.JsonTypeInfo.Type, out var example))
            schema.Example = example();

        return Task.CompletedTask;
    }
}
