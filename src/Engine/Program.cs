using Micser.Audio.Devices;
using Micser.Engine;
using Micser.Engine.Audio;
using Micser.Engine.Configuration;
using Micser.Engine.Contracts;
using Micser.Engine.Endpoints;
using Micser.Engine.Hubs;
using Micser.Engine.Modules;
using Micser.Engine.Security;
using Micser.Plugins.Main;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var engineOptions = builder.Configuration.GetSection(EngineOptions.SectionName).Get<EngineOptions>() ?? new EngineOptions();
using var instance = engineOptions.SingleInstance ? SingleInstance.TryAcquire() : null;
if (engineOptions.SingleInstance && instance == null)
{
    Console.Error.WriteLine("Another engine is already running in this session.");
    return 1;
}

builder.Services.Configure<EngineOptions>(builder.Configuration.GetSection(EngineOptions.SectionName));
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.CreateSchemaReferenceId = typeInfo =>
    typeInfo.Type.IsGenericType && typeInfo.Type.GetGenericTypeDefinition() == typeof(ModuleDto<>)
        ? typeInfo.Type.GenericTypeArguments[0].Name.Replace("State", "Module")
        : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo));

builder.Services.AddSingleton<AudioDeviceService>();
builder.Services.AddMainPlugin();
builder.Services.AddSingleton<ModuleCatalog>();
builder.Services.AddSingleton<EngineConfigStore>();
builder.Services.AddSingleton<AudioHost>();
builder.Services.AddHostedService<AudioHostService>();

builder.Services.AddSingleton<EngineAccess>();
builder.Services.AddHostedService<EngineDiscovery>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<ModuleDataSubscriptions>();
builder.Services.AddSingleton<IEngineNotifier, HubEngineNotifier>();
builder.Services.AddHostedService<ModuleDataPublisher>();

builder.Services.AddOptions<JsonOptions>().Configure<ModuleCatalog>((options, catalog) => EngineJson.Configure(options.SerializerOptions, catalog));
builder.Services.AddOptions<JsonHubProtocolOptions>().Configure<ModuleCatalog>((options, catalog) => EngineJson.Configure(options.PayloadSerializerOptions, catalog));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEngineAccess();
app.MapEngineApi();
app.MapHub<EngineHub>("/hubs/engine");

app.Run();
return 0;
