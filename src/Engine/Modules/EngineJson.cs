using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Micser.Engine.Modules;

/// <summary>
/// The JSON settings shared by the API, SignalR and the configuration file.
/// </summary>
public static class EngineJson
{
    public static void Configure(JsonSerializerOptions options, ModuleCatalog catalog)
    {
        options.AllowOutOfOrderMetadataProperties = true;
        options.Converters.Add(new JsonStringEnumConverter());
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(catalog.AddModuleTypes);
    }

    public static JsonSerializerOptions CreateOptions(ModuleCatalog catalog)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        Configure(options, catalog);
        return options;
    }
}
