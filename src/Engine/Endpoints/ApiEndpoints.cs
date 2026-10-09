using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Micser.Audio.Devices;
using Micser.Engine.Audio;
using Micser.Engine.Contracts;
using Micser.Engine.Plugins;

namespace Micser.Engine.Endpoints;

public sealed record HealthResponse(string Status);

public static class ApiEndpoints
{
    /// <summary>
    /// The largest plugin package the API accepts.
    /// </summary>
    public const long MaxPluginUploadBytes = 100L * 1024 * 1024;

    public static IEndpointRouteBuilder MapEngineApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").AddEndpointFilter(HandleEngineRequestException);

        MapHealth(api);
        MapModuleTypes(api);
        MapModules(api);
        MapConnections(api);
        MapDevices(api);
        MapEngine(api);
        MapPlugins(api);
        MapPortLayouts(api);
        MapPreferences(api);
        MapSubgraphs(api);
        MapSubgraphTemplates(api);

        return app;
    }

    private static async ValueTask<object?> HandleEngineRequestException(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        try
        {
            return await next(context);
        }
        catch (EngineRequestException ex) when (ex.Errors != null)
        {
            return TypedResults.ValidationProblem(ex.Errors, ex.Message);
        }
        catch (EngineRequestException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: ex.StatusCode);
        }
    }

    private static void MapConnections(RouteGroupBuilder api)
    {
        var connections = api.MapGroup("/connections").WithTags("Connections");

        connections.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetConnections())).WithName("GetConnections");

        connections
            .MapPost(
                "",
                (CreateConnectionRequest request, AudioHost host) =>
                {
                    var connection = host.Connect(request);
                    return TypedResults.Created($"/api/connections/{connection.Id}", connection);
                }
            )
            .WithName("CreateConnection");

        connections
            .MapDelete(
                "/{id:guid}",
                Results<NoContent, NotFound> (Guid id, AudioHost host) =>
                    host.Disconnect(id) ? TypedResults.NoContent() : TypedResults.NotFound()
            )
            .WithName("DeleteConnection");
    }

    private static void MapDevices(RouteGroupBuilder api)
    {
        api.MapGet(
                "/devices",
                (AudioDeviceService devices, DeviceDirection? direction, bool includeInactive = false) =>
                    TypedResults.Ok(
                        direction is { } d
                            ? devices.GetDevices(d, includeInactive)
                            :
                            [
                                .. devices.GetDevices(DeviceDirection.Input, includeInactive),
                                .. devices.GetDevices(DeviceDirection.Output, includeInactive),
                            ]
                    )
            )
            .WithName("GetDevices")
            .WithTags("Devices");
    }

    private static void MapEngine(RouteGroupBuilder api)
    {
        var engine = api.MapGroup("/engine").WithTags("Engine");

        engine.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetStatus())).WithName("GetEngineStatus");
        engine.MapPost("/start", (AudioHost host) => TypedResults.Ok(host.Start())).WithName("StartEngine");
        engine.MapPost("/stop", (AudioHost host) => TypedResults.Ok(host.Stop())).WithName("StopEngine");
        engine
            .MapPost("/restart-audio", (AudioHost host) => TypedResults.Ok(host.RestartAudio()))
            .WithName("RestartAudio")
            .WithDescription("Rebuilds the audio graph, which reopens all device streams with fresh buffers.");
        engine
            .MapPost(
                "/shutdown",
                (IHostApplicationLifetime lifetime) =>
                {
                    // after the response: the shutdown stops the server
                    lifetime.StopApplication();
                    return TypedResults.Accepted((string?)null);
                }
            )
            .WithName("ShutdownEngine")
            .WithDescription("Stops the engine process gracefully; used by the shell's exit command.");
        engine
            .MapPut(
                "/settings",
                (EngineSettingsDto settings, AudioHost host) => TypedResults.Ok(host.UpdateSettings(settings))
            )
            .WithName("UpdateEngineSettings");
    }

    private static void MapHealth(RouteGroupBuilder api)
    {
        api.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok"))).WithName("GetHealth").WithTags("Engine");
    }

    private static void MapModules(RouteGroupBuilder api)
    {
        var modules = api.MapGroup("/modules").WithTags("Modules");

        modules.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetModules())).WithName("GetModules");

        modules
            .MapGet(
                "/{id:guid}",
                Results<Ok<ModuleDto>, NotFound> (Guid id, AudioHost host) =>
                    host.GetModule(id) is { } module ? TypedResults.Ok(module) : TypedResults.NotFound()
            )
            .WithName("GetModule");

        modules
            .MapPost(
                "",
                (CreateModuleRequest request, AudioHost host) =>
                {
                    var module = host.AddModule(request);
                    return TypedResults.Created($"/api/modules/{module.Id}", module);
                }
            )
            .WithName("CreateModule");

        modules
            .MapPut(
                "/{id:guid}",
                (Guid id, ModuleDto module, AudioHost host) => TypedResults.Ok(host.UpdateModule(id, module))
            )
            .WithName("UpdateModule");

        modules
            .MapDelete(
                "/{id:guid}",
                Results<NoContent, NotFound> (Guid id, AudioHost host) =>
                    host.RemoveModule(id) ? TypedResults.NoContent() : TypedResults.NotFound()
            )
            .WithName("DeleteModule");
    }

    private static void MapModuleTypes(RouteGroupBuilder api)
    {
        api.MapGet("/module-types", (AudioHost host) => TypedResults.Ok(host.GetModuleTypes()))
            .WithName("GetModuleTypes")
            .WithTags("Modules");
    }

    private static void MapPlugins(RouteGroupBuilder api)
    {
        var plugins = api.MapGroup("/plugins").WithTags("Plugins");

        plugins.MapGet("", (PluginService service) => TypedResults.Ok(service.GetPlugins())).WithName("GetPlugins");

        // the token protects the API, so there's no antiforgery token for the upload
        plugins
            .MapPost(
                "",
                async (IFormFile package, PluginService service) =>
                {
                    await using var stream = new MemoryStream();
                    await package.CopyToAsync(stream);
                    stream.Position = 0;
                    return TypedResults.Ok(service.Install(stream));
                }
            )
            .WithName("InstallPlugin")
            .WithDescription(
                "Stages a plugin package (a zip with plugin.json at its root) for installation when the engine restarts."
            )
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxPluginUploadBytes));

        plugins
            .MapDelete(
                "/{id}",
                Results<NoContent, NotFound> (string id, PluginService service) =>
                    service.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound()
            )
            .WithName("RemovePlugin")
            .WithDescription(
                "Stages a user plugin for removal when the engine restarts, or cancels its staged installation."
            );
    }

    private static void MapPortLayouts(RouteGroupBuilder api)
    {
        api.MapGet("/port-layouts", (AudioHost host) => TypedResults.Ok(host.GetPortLayouts()))
            .WithName("GetPortLayouts")
            .WithTags("Modules")
            .WithDescription(
                "The channel layouts of all modules' ports in the last processed block; changes are pushed as PortLayoutsChanged."
            );
    }

    private static void MapPreferences(RouteGroupBuilder api)
    {
        var preferences = api.MapGroup("/preferences").WithTags("Preferences");

        preferences.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetPreferences())).WithName("GetPreferences");
        preferences
            .MapPut(
                "",
                (UiPreferencesDto preferences, AudioHost host) => TypedResults.Ok(host.UpdatePreferences(preferences))
            )
            .WithName("UpdatePreferences");
    }

    private static void MapSubgraphs(RouteGroupBuilder api)
    {
        var subgraphs = api.MapGroup("/subgraphs").WithTags("Subgraphs");

        subgraphs.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetSubgraphs())).WithName("GetSubgraphs");

        subgraphs
            .MapPost(
                "",
                (CreateSubgraphRequest request, AudioHost host) =>
                {
                    var subgraph = host.AddSubgraph(request);
                    return TypedResults.Created($"/api/subgraphs/{subgraph.Id}", subgraph);
                }
            )
            .WithName("CreateSubgraph")
            .WithDescription("Creates a subgraph and moves the modules into it.");

        subgraphs
            .MapPut(
                "/{id:guid}",
                (Guid id, SubgraphDto subgraph, AudioHost host) => TypedResults.Ok(host.UpdateSubgraph(id, subgraph))
            )
            .WithName("UpdateSubgraph");

        subgraphs
            .MapDelete(
                "/{id:guid}",
                Results<NoContent, NotFound> (Guid id, AudioHost host, bool deleteModules = false) =>
                    host.RemoveSubgraph(id, deleteModules) ? TypedResults.NoContent() : TypedResults.NotFound()
            )
            .WithName("DeleteSubgraph")
            .WithDescription(
                "Removes a subgraph. Its modules stay in the graph, or are removed with their connections if deleteModules is true."
            );

        subgraphs
            .MapPost(
                "/{id:guid}/update-from-template",
                (Guid id, AudioHost host) => TypedResults.Ok(host.UpdateSubgraphFromTemplate(id))
            )
            .WithName("UpdateSubgraphFromTemplate")
            .WithDescription("Makes the subgraph match its template's current revision.");
    }

    private static void MapSubgraphTemplates(RouteGroupBuilder api)
    {
        var templates = api.MapGroup("/subgraph-templates").WithTags("Subgraph templates");

        templates.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetTemplates())).WithName("GetSubgraphTemplates");

        templates
            .MapPost(
                "",
                (SaveSubgraphTemplateRequest request, AudioHost host) => TypedResults.Ok(host.SaveTemplate(request))
            )
            .WithName("SaveSubgraphTemplate")
            .WithDescription("Saves a subgraph as a new template, or over the template with the given id.");

        templates
            .MapPut(
                "/{id:guid}/name",
                (Guid id, RenameSubgraphTemplateRequest request, AudioHost host) =>
                    TypedResults.Ok(host.RenameTemplate(id, request))
            )
            .WithName("RenameSubgraphTemplate");

        templates
            .MapDelete(
                "/{id:guid}",
                Results<NoContent, NotFound> (Guid id, AudioHost host) =>
                    host.RemoveTemplate(id) ? TypedResults.NoContent() : TypedResults.NotFound()
            )
            .WithName("DeleteSubgraphTemplate")
            .WithDescription("Removes a template; the subgraphs created from it stay.");

        templates
            .MapPost(
                "/{id:guid}/instantiate",
                (Guid id, InstantiateSubgraphTemplateRequest request, AudioHost host) =>
                {
                    var subgraph = host.InstantiateTemplate(id, request);
                    return TypedResults.Created($"/api/subgraphs/{subgraph.Id}", subgraph);
                }
            )
            .WithName("InstantiateSubgraphTemplate")
            .WithDescription("Creates a subgraph with the template's modules and connections.");
    }
}
