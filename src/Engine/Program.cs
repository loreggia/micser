using Micser.Audio.Devices;
using Micser.Engine;
using Micser.Engine.Audio;
using Micser.Engine.Configuration;
using Micser.Engine.Endpoints;
using Micser.Engine.Hubs;
using Micser.Engine.Modules;
using Micser.Engine.Security;
using Micser.Plugins.Main;
using Micser.ServiceDefaults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var engineOptions = builder.Configuration.GetSection(EngineOptions.SectionName).Get<EngineOptions>() ?? new EngineOptions();

// the build-time OpenAPI generator runs this code up to Build() while an engine may be running
var isGeneratingDocument = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
var checkSingleInstance = engineOptions.SingleInstance && !isGeneratingDocument;

using var instance = checkSingleInstance ? SingleInstance.TryAcquire() : null;
if (checkSingleInstance && instance == null)
{
    Console.Error.WriteLine("Another engine is already running in this session.");
    return 1;
}

builder.Services.Configure<EngineOptions>(builder.Configuration.GetSection(EngineOptions.SectionName));

// Serilog writes its own sinks and forwards to the remaining providers (OpenTelemetry)
builder.Logging.ClearProviders();
builder.AddServiceDefaults();
builder.Services.AddSerilog(
    (services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services),
    writeToProviders: true);

builder.Services.AddProblemDetails();
builder.Services.AddEngineOpenApi();

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
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapEngineApi();
app.MapHub<EngineHub>("/hubs/engine");

// the web UI (copied to wwwroot on publish) handles all other paths
app.MapFallbackToFile("{*path:nonfile:regex(^(?!api/|hubs/).*$)}", "index.html");

app.Run();
return 0;
