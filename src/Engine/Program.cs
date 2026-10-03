using Micser.Audio.Devices;
using Micser.Engine;
using Micser.Engine.Audio;
using Micser.Engine.Configuration;
using Micser.Engine.Endpoints;
using Micser.Engine.Hubs;
using Micser.Engine.Modules;
using Micser.Engine.Plugins;
using Micser.Engine.Security;
using Micser.ServiceDefaults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var engineOptions = builder.Configuration.GetSection(EngineOptions.SectionName).Get<EngineOptions>() ?? new EngineOptions();

// the build-time OpenAPI generator starts the host (without a server) while an engine may be running
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
builder.Services.AddSingleton<ISystemVolume, SystemVolume>();
// the engine's folder also while the build-time OpenAPI generator runs it; the document only describes the built-in plugins
var engineDirectory = Path.GetDirectoryName(typeof(EngineOptions).Assembly.Location) ?? AppContext.BaseDirectory;
var plugins = PluginLoader.Load(builder.Services, Path.Combine(engineDirectory, engineOptions.BuiltInPluginsPath), isGeneratingDocument ? null : engineOptions.PluginsPath);
builder.Services.AddSingleton(plugins);
builder.Services.AddSingleton(new PluginInstaller(engineOptions.PluginsPath, plugins));
builder.Services.AddSingleton<PluginService>();
builder.Services.AddSingleton<ModuleCatalog>();
builder.Services.AddSingleton<EngineConfigStore>();
builder.Services.AddSingleton<AudioHost>();
builder.Services.AddSingleton<EngineAccess>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ModuleDataSubscriptions>();
builder.Services.AddSingleton<IEngineNotifier, HubEngineNotifier>();

// the audio engine, discovery file and data pushes must not run while generating the OpenAPI document
if (!isGeneratingDocument)
{
    builder.Services.AddHostedService<AudioHostService>();
    builder.Services.AddHostedService<EngineDiscovery>();
    builder.Services.AddHostedService<ModuleDataPublisher>();
}

builder.Services.AddOptions<JsonOptions>().Configure<ModuleCatalog>((options, catalog) => EngineJson.Configure(options.SerializerOptions, catalog));
builder.Services.AddOptions<JsonHubProtocolOptions>().Configure<ModuleCatalog>((options, catalog) => EngineJson.Configure(options.PayloadSerializerOptions, catalog));

var app = builder.Build();

plugins.LogResults(app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(PluginLoader)));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEngineAccess();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UsePluginFiles(plugins);
app.MapEngineApi();
app.MapHub<EngineHub>("/hubs/engine");

// the web UI (copied to wwwroot on publish) handles all other paths
app.MapFallbackToFile("{*path:nonfile:regex(^(?!api/|hubs/|plugins/).*$)}", "index.html");

app.Run();
return 0;
