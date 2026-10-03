using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Micser.Audio.Devices;
using Micser.Engine.Contracts;
using Micser.Engine.Modules;
using Micser.Engine.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.Engine.Tests;

/// <summary>
/// An engine with its own configuration directory and the access token required, as in production.
/// </summary>
internal sealed class EngineFactory : WebApplicationFactory<Program>
{
    private readonly bool _ownsDirectory;

    /// <param name="directory">The configuration directory; a new temporary one (deleted on dispose) if null.</param>
    public EngineFactory(string? directory = null)
    {
        _ownsDirectory = directory == null;
        Directory = directory ?? CreateTemporaryDirectory();
        System.IO.Directory.CreateDirectory(Directory);
    }

    public string ConfigPath => Path.Combine(Directory, "config.json");

    public string Directory { get; }

    /// <summary>
    /// The user plugin folder. The built-in Main plugin comes from the test's output folder.
    /// </summary>
    public string PluginsPath => Path.Combine(Directory, "plugins");

    public JsonSerializerOptions Json => EngineJson.CreateOptions(Services.GetRequiredService<ModuleCatalog>());

    /// <summary>
    /// Replaces the volume of the machine's default output device.
    /// </summary>
    public FakeSystemVolume SystemVolume { get; } = new();

    public string Token => Services.GetRequiredService<EngineAccess>().Token;

    public static string CreateTemporaryDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "micser-tests", Guid.NewGuid().ToString("N"));
    }

    public async Task<ModuleDto> AddModuleAsync(HttpClient client, string type)
    {
        var response = await client.PostAsJsonAsync("/api/modules", new CreateModuleRequest(type), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ModuleDto>(Json))!;
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return client;
    }

    public HubConnection CreateHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, "/hubs/engine"), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(Token);
            })
            .AddJsonProtocol(options => EngineJson.Configure(options.PayloadSerializerOptions, Services.GetRequiredService<ModuleCatalog>()))
            .Build();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_ownsDirectory)
        {
            try
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch (UnauthorizedAccessException)
            {
                // loaded user plugin assemblies stay locked until the process exits
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Engine:ConfigPath", ConfigPath);
        builder.UseSetting("Engine:DiscoveryPath", Path.Combine(Directory, "engine.json"));
        builder.UseSetting("Engine:PluginsPath", PluginsPath);
        builder.UseSetting("Engine:SingleInstance", "false");
        builder.UseSetting("Engine:RequireToken", "true");
        builder.ConfigureTestServices(services => services.AddSingleton<ISystemVolume>(SystemVolume));
    }
}

internal static class HubClientExtensions
{
    /// <summary>
    /// Completes with the first message of a hub method matching the predicate.
    /// </summary>
    public static Task<T> NextAsync<T>(this HubConnection connection, string method, Func<T, bool>? predicate = null)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        IDisposable? subscription = null;
        subscription = connection.On<T>(method, value =>
        {
            if (predicate?.Invoke(value) ?? true)
            {
                result.TrySetResult(value);
                subscription?.Dispose();
            }
        });

        return result.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
