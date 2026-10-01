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
        var connections = api.MapGroup("/connections");

        connections.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetConnections()));

        connections.MapPost("", (CreateConnectionRequest request, AudioHost host) =>
        {
            var connection = host.Connect(request);
            return TypedResults.Created($"/api/connections/{connection.Id}", connection);
        });

        connections.MapDelete("/{id:guid}", Results<NoContent, NotFound> (Guid id, AudioHost host) =>
            host.Disconnect(id) ? TypedResults.NoContent() : TypedResults.NotFound());
    }

    private static void MapDevices(RouteGroupBuilder api)
    {
        api.MapGet("/devices", (AudioDeviceService devices, DeviceDirection? direction, bool includeInactive = false) =>
            TypedResults.Ok(direction is { } d
                ? devices.GetDevices(d, includeInactive)
                : [.. devices.GetDevices(DeviceDirection.Input, includeInactive), .. devices.GetDevices(DeviceDirection.Output, includeInactive)]));
    }

    private static void MapEngine(RouteGroupBuilder api)
    {
        var engine = api.MapGroup("/engine");

        engine.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetStatus()));
        engine.MapPost("/start", (AudioHost host) => TypedResults.Ok(host.Start()));
        engine.MapPost("/stop", (AudioHost host) => TypedResults.Ok(host.Stop()));
        engine.MapPut("/settings", (EngineSettingsDto settings, AudioHost host) => TypedResults.Ok(host.UpdateSettings(settings)));
    }

    private static void MapHealth(RouteGroupBuilder api)
    {
        api.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok")));
    }

    private static void MapModules(RouteGroupBuilder api)
    {
        var modules = api.MapGroup("/modules");

        modules.MapGet("", (AudioHost host) => TypedResults.Ok(host.GetModules()));

        modules.MapGet("/{id:guid}", Results<Ok<ModuleDto>, NotFound> (Guid id, AudioHost host) =>
            host.GetModule(id) is { } module ? TypedResults.Ok(module) : TypedResults.NotFound());

        modules.MapPost("", (CreateModuleRequest request, AudioHost host) =>
        {
            var module = host.AddModule(request);
            return TypedResults.Created($"/api/modules/{module.Id}", module);
        });

        modules.MapPut("/{id:guid}", (Guid id, ModuleDto module, AudioHost host) => TypedResults.Ok(host.UpdateModule(id, module)));

        modules.MapDelete("/{id:guid}", Results<NoContent, NotFound> (Guid id, AudioHost host) =>
            host.RemoveModule(id) ? TypedResults.NoContent() : TypedResults.NotFound());
    }

    private static void MapModuleTypes(RouteGroupBuilder api)
    {
        api.MapGet("/module-types", (AudioHost host) => TypedResults.Ok(host.GetModuleTypes()));
    }
}
