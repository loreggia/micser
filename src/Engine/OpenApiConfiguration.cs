using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
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
}
