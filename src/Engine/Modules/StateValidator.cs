using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Micser.Engine.Modules;

/// <summary>
/// Validates data annotations on an object and, recursively, on the objects in its properties and collections.
/// Attributes on the primary constructor parameters of records count for the property of the same name, as in ASP.NET Core.
/// </summary>
public static class StateValidator
{
    private static readonly ConcurrentDictionary<
        Type,
        (PropertyInfo Property, ValidationAttribute[] Attributes)[]
    > ParameterAttributes = new();

    /// <summary>
    /// Returns the errors keyed by camelCase property path (e.g. <c>state.bands[0].frequency</c>); empty if valid.
    /// </summary>
    public static Dictionary<string, string[]> Validate(object instance, string path = "")
    {
        var errors = new Dictionary<string, List<string>>();
        Validate(instance, path, errors, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }

    private static string Combine(string path, string name)
    {
        var camelCase = JsonNamingPolicy.CamelCase.ConvertName(name);
        return path.Length == 0 ? camelCase : $"{path}.{camelCase}";
    }

    /// <summary>
    /// Returns the validation attributes of the constructor parameters that initialize properties of the same name, i.e.
    /// a record's primary constructor.
    /// </summary>
    private static (PropertyInfo Property, ValidationAttribute[] Attributes)[] GetParameterAttributes(Type type)
    {
        return ParameterAttributes.GetOrAdd(
            type,
            static type =>
            {
                var properties = type.GetProperties().ToDictionary(p => p.Name, StringComparer.Ordinal);
                var constructor = type.GetConstructors()
                    .Where(c =>
                        c.GetParameters() is { Length: > 0 } parameters
                        && parameters.All(p => properties.ContainsKey(p.Name!))
                    )
                    .MaxBy(c => c.GetParameters().Length);

                return constructor == null
                    ? []
                    :
                    [
                        .. constructor
                            .GetParameters()
                            .Select(p =>
                                (
                                    Property: properties[p.Name!],
                                    Attributes: p.GetCustomAttributes<ValidationAttribute>().ToArray()
                                )
                            )
                            .Where(p => p.Attributes.Length > 0),
                    ];
            }
        );
    }

    private static bool IsLeaf(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid);
    }

    private static void Validate(
        object instance,
        string path,
        Dictionary<string, List<string>> errors,
        HashSet<object> visited
    )
    {
        if (IsLeaf(instance.GetType()) || !visited.Add(instance))
        {
            return;
        }

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        foreach (var (property, attributes) in GetParameterAttributes(instance.GetType()))
        {
            var context = new ValidationContext(instance) { MemberName = property.Name, DisplayName = property.Name };
            Validator.TryValidateValue(property.GetValue(instance), context, results, attributes);
        }

        foreach (var result in results)
        {
            foreach (var member in result.MemberNames.DefaultIfEmpty(""))
            {
                var key = member.Length == 0 ? path : Combine(path, member);
                if (!errors.TryGetValue(key, out var messages))
                {
                    errors[key] = messages = [];
                }

                messages.Add(result.ErrorMessage ?? "Invalid value.");
            }
        }

        foreach (var property in instance.GetType().GetProperties())
        {
            if (
                property.GetIndexParameters().Length > 0
                || IsLeaf(property.PropertyType)
                || property.IsDefined(typeof(JsonIgnoreAttribute))
            )
            {
                continue;
            }

            var value = property.GetValue(instance);
            var propertyPath = Combine(path, property.Name);

            if (value is IEnumerable items and not string)
            {
                var index = 0;
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        Validate(item, $"{propertyPath}[{index}]", errors, visited);
                    }

                    index++;
                }
            }
            else if (value != null)
            {
                Validate(value, propertyPath, errors, visited);
            }
        }
    }
}
