using System.Net;
using System.Net.Http.Json;
using Micser.Engine.Contracts;
using Micser.Engine.Plugins;

namespace Micser.Engine.Tests;

public class PluginApiTests
{
    [Test]
    public async Task GetPlugins_ListsTheBuiltInMainPlugin()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var plugins = await client.GetFromJsonAsync<PluginDto[]>("/api/plugins", factory.Json);

        await Assert.That(plugins).IsEquivalentTo([new PluginDto("Main", "Main", "1.0.0", true, true, null, "/plugins/Main/web/index.js", PluginChange.None)]);
    }

    [Test]
    public async Task InstallPlugin_IsStagedAndBroadcast()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        await using var hub = factory.CreateHubConnection();
        var changed = hub.NextAsync<PluginDto[]>("PluginsChanged");
        await hub.StartAsync();

        var response = await client.PostAsync("/api/plugins", CreateUpload(PluginLoaderTests.CopyTestPlugin(factory.Directory, "package")));
        response.EnsureSuccessStatusCode();
        var installed = await response.Content.ReadFromJsonAsync<PluginDto>(factory.Json);

        var expected = new PluginDto("Test", "Test plugin", "1.2.3", false, false, null, null, PluginChange.Install);
        await Assert.That(installed).IsEqualTo(expected);
        await Assert.That((await changed).Select(p => p.Id)).IsEquivalentTo(["Main", "Test"]);

        // removing a staged plugin cancels its installation
        (await client.DeleteAsync("/api/plugins/Test")).EnsureSuccessStatusCode();
        var plugins = await client.GetFromJsonAsync<PluginDto[]>("/api/plugins", factory.Json);
        await Assert.That(plugins!.Select(p => p.Id)).IsEquivalentTo(["Main"]);
    }

    [Test]
    public async Task InstallPlugin_InvalidPackage_IsRejected()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        using var content = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "package", "plugin.zip" } };

        var response = await client.PostAsync("/api/plugins", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RemovePlugin_BuiltInOrMissing_IsRejected()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        await Assert.That((await client.DeleteAsync("/api/plugins/Main")).StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That((await client.DeleteAsync("/api/plugins/Missing")).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UserPlugin_IsLoadedAndServesItsWidgetsWithoutToken()
    {
        await using var factory = new EngineFactory();
        var directory = PluginLoaderTests.CopyTestPlugin(factory.PluginsPath);
        await File.WriteAllTextAsync(
            Path.Combine(directory, PluginManifest.FileName),
            """{ "id": "Test", "name": "Test plugin", "version": "1.2.3", "assembly": "Micser.Engine.TestPlugin.dll", "web": "web/index.js" }""");
        Directory.CreateDirectory(Path.Combine(directory, "web"));
        await File.WriteAllTextAsync(Path.Combine(directory, "web", "index.js"), "export default { name: 'Test', widgets: [] };");
        using var client = factory.CreateClient();
        using var authorized = factory.CreateAuthorizedClient();

        var plugins = await authorized.GetFromJsonAsync<PluginDto[]>("/api/plugins", factory.Json);
        var moduleTypes = await authorized.GetFromJsonAsync<ModuleTypeDto[]>("/api/module-types", factory.Json);
        var script = await client.GetAsync("/plugins/Test/web/index.js");
        var assembly = await client.GetAsync("/plugins/Test/Micser.Engine.TestPlugin.dll");

        await Assert.That(plugins!.Single(p => p.Id == "Test").WebUrl).IsEqualTo("/plugins/Test/web/index.js");
        await Assert.That(moduleTypes!.Select(t => t.Type)).Contains("Test");
        await Assert.That(script.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(script.Content.Headers.ContentType?.MediaType).IsEqualTo("text/javascript");
        await Assert.That(script.Headers.CacheControl?.NoCache).IsTrue();
        await Assert.That(assembly.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private static MultipartFormDataContent CreateUpload(string pluginDirectory)
    {
        var package = PluginLoaderTests.CreatePackage(pluginDirectory);
        return new MultipartFormDataContent { { new StreamContent(package), "package", "plugin.zip" } };
    }
}
