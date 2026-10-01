using System.Net.Http.Json;
using Micser.Engine.Contracts;
using Micser.Plugins.Main.Modules;

namespace Micser.Engine.Tests;

public class ConfigurationTests
{
    [Test]
    public async Task Configuration_IsRestoredAfterRestart()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        try
        {
            ModuleDto gain;
            ConnectionDto? connection;
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var input = await factory.AddModuleAsync(client, "DeviceInput");
                gain = await factory.AddModuleAsync(client, "Gain");
                (await client.PutAsJsonAsync<ModuleDto>($"/api/modules/{gain.Id}", ((ModuleDto<GainState>)gain) with { State = new GainState(-3f) }, factory.Json)).EnsureSuccessStatusCode();
                var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(input.Id, "Output", gain.Id, "Input"));
                connection = await response.Content.ReadFromJsonAsync<ConnectionDto>();
            }

            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var restored = await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{gain.Id}", factory.Json);
                var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

                await Assert.That(((ModuleDto<GainState>)restored!).State.Gain).IsEqualTo(-3f);
                await Assert.That(connections).IsEquivalentTo([connection!]);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task Configuration_Unreadable_IsBackedUpAndEngineStartsEmpty()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "config.json"), "{ not json");
        try
        {
            await using var factory = new EngineFactory(directory);
            using var client = factory.CreateAuthorizedClient();

            var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

            await Assert.That(modules).IsEmpty();
            await Assert.That(Directory.GetFiles(directory, "config.json.*.bak")).HasSingleItem();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task Configuration_SkipsUnknownModulesAndTheirConnections()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        Directory.CreateDirectory(directory);
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        await File.WriteAllTextAsync(Path.Combine(directory, "config.json"), $$"""
            {
              "version": 1,
              "modules": [
                { "type": "Removed", "id": "{{unknown}}", "state": {} },
                { "type": "Gain", "id": "{{known}}", "state": { "gain": 3 } }
              ],
              "connections": [
                { "id": "{{Guid.NewGuid()}}", "sourceModuleId": "{{unknown}}", "sourcePort": "Output", "targetModuleId": "{{known}}", "targetPort": "Input" }
              ]
            }
            """);
        try
        {
            await using var factory = new EngineFactory(directory);
            using var client = factory.CreateAuthorizedClient();

            var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);
            var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

            await Assert.That(modules!.Select(m => m.Id)).IsEquivalentTo([known]);
            await Assert.That(connections).IsEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
