using System.Net.Http.Json;
using System.Text.Json.Nodes;
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
    public async Task Configuration_RestoresChannelConnectionsAndShowsTheirChannels()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        try
        {
            ModuleDto source;
            ModuleDto target;
            ConnectionDto? connection;
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                source = await factory.AddModuleAsync(client, "Gain");
                target = await factory.AddModuleAsync(client, "Gain");
                (await client.PutAsJsonAsync($"/api/modules/{target.Id}", target with { ChannelCount = 4 }, factory.Json)).EnsureSuccessStatusCode();
                var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(source.Id, "Output", target.Id, "Input", 1, 3));
                connection = await response.Content.ReadFromJsonAsync<ConnectionDto>();
            }

            // a configuration written otherwise, e.g. by hand, gets the channels shown and the channel count the connections need
            var config = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "config.json")))!;
            foreach (var module in config["modules"]!.AsArray())
            {
                module!["showChannels"] = false;
                module["channelCount"] = module["id"]!.GetValue<Guid>() == target.Id ? 2 : null;
            }

            await File.WriteAllTextAsync(Path.Combine(directory, "config.json"), config.ToJsonString());

            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var modules = (await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json))!.ToDictionary(m => m.Id);
                var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

                await Assert.That(connections).IsEquivalentTo([connection!]);
                await Assert.That(modules[source.Id].ShowChannels).IsTrue();
                await Assert.That(modules[target.Id].ShowChannels).IsTrue();
                await Assert.That(modules[target.Id].ChannelCount).IsEqualTo(4);
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
    public async Task Configuration_KeepsUnknownModulesAndTheirConnectionsOutOfTheGraph()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        Directory.CreateDirectory(directory);
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        var connection = Guid.NewGuid();
        await File.WriteAllTextAsync(Path.Combine(directory, "config.json"), $$"""
            {
              "version": 1,
              "modules": [
                { "type": "Removed", "id": "{{unknown}}", "state": {} },
                { "type": "Gain", "id": "{{known}}", "state": { "gain": 3 } }
              ],
              "connections": [
                { "id": "{{connection}}", "sourceModuleId": "{{unknown}}", "sourcePort": "Output", "targetModuleId": "{{known}}", "targetPort": "Input" }
              ]
            }
            """);
        try
        {
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();

                var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);
                var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

                await Assert.That(modules!.Select(m => m.Id)).IsEquivalentTo([known]);
                await Assert.That(connections).IsEmpty();

                // saves the configuration
                var gain = (ModuleDto<GainState>)modules![0];
                (await client.PutAsJsonAsync<ModuleDto>($"/api/modules/{known}", gain with { State = new GainState(-3f) }, factory.Json)).EnsureSuccessStatusCode();
            }

            var saved = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "config.json")))!;
            var savedModules = saved["modules"]!.AsArray();

            await Assert.That(savedModules.Select(m => (string)m!["type"]!)).IsEquivalentTo(["Gain", "Removed"]);
            await Assert.That(saved["connections"]!.AsArray().Select(c => (string)c!["id"]!)).IsEquivalentTo([connection.ToString()]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
