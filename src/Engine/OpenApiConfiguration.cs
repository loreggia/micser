using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Micser.Engine.Contracts;

namespace Micser.Engine;

public static class OpenApiConfiguration
{
    /// <summary>
    /// Adds the OpenAPI document the web SDK generates its client from.
    /// </summary>
    public static IServiceCollection AddEngineOpenApi(this IServiceCollection services)
    {
        return services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = typeInfo =>
                typeInfo.Type.IsGenericType && typeInfo.Type.GetGenericTypeDefinition() == typeof(ModuleDto<>)
                    ? typeInfo.Type.GenericTypeArguments[0].Name.Replace("State", "Module")
                    : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);

            // The engine always writes non-nullable properties, so clients can rely on them even where C# has defaults.
            options.AddSchemaTransformer(
                (schema, context, _) =>
                {
                    if (
                        context.JsonTypeInfo.Kind != JsonTypeInfoKind.Object
                        || schema.Properties is not { Count: > 0 } properties
                    )
                    {
                        return Task.CompletedTask;
                    }

                    var nullability = new NullabilityInfoContext();
                    foreach (var property in context.JsonTypeInfo.Properties)
                    {
                        if (properties.ContainsKey(property.Name) && !IsNullable(property, nullability))
                        {
                            schema.Required ??= new HashSet<string>();
                            schema.Required.Add(property.Name);
                        }
                    }

                    return Task.CompletedTask;
                }
            );

            // the XML comment generator writes multi-line descriptions with the platform's line breaks (CRLF on Windows)
            options.AddDocumentTransformer(
                (document, _, _) =>
                {
                    new OpenApiWalker(new LineEndingNormalizer()).Walk(document);
                    return Task.CompletedTask;
                }
            );
        });
    }

    private static bool IsNullable(JsonPropertyInfo property, NullabilityInfoContext nullability)
    {
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        return property.AttributeProvider is not PropertyInfo info
            || nullability.Create(info).ReadState != NullabilityState.NotNull;
    }

    /// <summary>
    /// Replaces CRLF line breaks in descriptions with LF, so the document is the same whichever platform builds it.
    /// </summary>
    private sealed class LineEndingNormalizer : OpenApiVisitorBase
    {
        public override void Visit(IOpenApiSchema schema)
        {
            Normalize(schema);
        }

        public override void Visit(OpenApiOperation operation)
        {
            Normalize(operation);
            if (operation.Summary?.Contains('\r') == true)
            {
                operation.Summary = operation.Summary.ReplaceLineEndings("\n");
            }
        }

        public override void Visit(IOpenApiParameter parameter)
        {
            Normalize(parameter);
        }

        public override void Visit(IOpenApiRequestBody requestBody)
        {
            Normalize(requestBody);
        }

        public override void Visit(IOpenApiResponse response)
        {
            Normalize(response);
        }

        private static void Normalize(object element)
        {
            if (
                element is IOpenApiDescribedElement { Description: { } description } described
                && description.Contains('\r')
            )
            {
                described.Description = description.ReplaceLineEndings("\n");
            }
        }
    }
}
