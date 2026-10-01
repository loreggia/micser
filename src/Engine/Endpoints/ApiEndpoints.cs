using Micser.Audio.Devices;
using Micser.Engine.Audio;
using Micser.Engine.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Micser.Engine.Endpoints;

public sealed record HealthResponse(string Status);

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapEngineApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").AddEndpointFilter(HandleEngineRequestException);

        MapHealth(api);
        MapModuleTypes(api);
        MapModules(api);
        MapConnections(api);
        MapDevices(api);
        MapEngine(api);

        return app;
    }

    private static async ValueTask<object?> HandleEngineRequestException(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
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

        connections.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetConnections()))
            .WithName("GetConnections");

        connections.MapPost("", (CreateConnectionRequest request, AudioHost host) =>
        {
            var connection = host.Connect(request);
            return TypedResults.Created($"/api/connections/{connection.Id}", connection);
        }).WithName("CreateConnection");

        connections.MapDelete("/{id:guid}", Results<NoContent, NotFound> (Guid id, AudioHost host) =>
            host.Disconnect(id) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithName("DeleteConnection");
    }

    private static void MapDevices(RouteGroupBuilder api)
    {
        api.MapGet("/devices", (AudioDeviceService devices, DeviceDirection? direction, bool includeInactive = false) =>
            TypedResults.Ok(direction is { } d
                ? devices.GetDevices(d, includeInactive)
                : [.. devices.GetDevices(DeviceDirection.Input, includeInactive), .. devices.GetDevices(DeviceDirection.Output, includeInactive)]))
            .WithName("GetDevices")
            .WithTags("Devices");
    }

    private static void MapEngine(RouteGroupBuilder api)
    {
        var engine = api.MapGroup("/engine").WithTags("Engine");

        engine.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetStatus())).WithName("GetEngineStatus");
        engine.MapPost("/start", (AudioHost host) => TypedResults.Ok(host.Start())).WithName("StartEngine");
        engine.MapPost("/stop", (AudioHost host) => TypedResults.Ok(host.Stop())).WithName("StopEngine");
        engine.MapPost("/shutdown", (IHostApplicationLifetime lifetime) =>
        {
            // after the response: the shutdown stops the server
            lifetime.StopApplication();
            return TypedResults.Accepted((string?)null);
        }).WithName("ShutdownEngine").WithDescription("Stops the engine process gracefully; used by the shell's exit command.");
        engine.MapPut("/settings", (EngineSettingsDto settings, AudioHost host) => TypedResults.Ok(host.UpdateSettings(settings))).WithName("UpdateEngineSettings");
    }

    private static void MapHealth(RouteGroupBuilder api)
    {
        api.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok"))).WithName("GetHealth").WithTags("Engine");
    }

    private static void MapModules(RouteGroupBuilder api)
    {
        var modules = api.MapGroup("/modules").WithTags("Modules");

        modules.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetModules())).WithName("GetModules");

        modules.MapGet("/{id:guid}", Results<Ok<ModuleDto>, NotFound> (Guid id, AudioHost host) =>
            host.GetModule(id) is { } module ? TypedResults.Ok(module) : TypedResults.NotFound())
            .WithName("GetModule");

        modules.MapPost("", (CreateModuleRequest request, AudioHost host) =>
        {
            var module = host.AddModule(request);
            return TypedResults.Created($"/api/modules/{module.Id}", module);
        }).WithName("CreateModule");

        modules.MapPut("/{id:guid}", (Guid id, ModuleDto module, AudioHost host) => TypedResults.Ok(host.UpdateModule(id, module))).WithName("UpdateModule");

        modules.MapDelete("/{id:guid}", Results<NoContent, NotFound> (Guid id, AudioHost host) =>
            host.RemoveModule(id) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithName("DeleteModule");
    }

    private static void MapModuleTypes(RouteGroupBuilder api)
    {
        api.MapGet("/module-types", (AudioHost host) => TypedResults.Ok(host.GetModuleTypes())).WithName("GetModuleTypes").WithTags("Modules");
    }
}
