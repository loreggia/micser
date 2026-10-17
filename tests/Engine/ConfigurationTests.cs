using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Micser.Engine.Contracts;
using Micser.Plugins.Main.Modules;
using Micser.Tests.Common;

namespace Micser.Engine.Tests;

public class ConfigurationTests
{
    [Test]
    public async Task Configuration_IsRestoredAfterRestart()
    {
        using var directory = new TempDirectory();
        ModuleDto gain;
        ConnectionDto? connection;
        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();
            var input = await factory.AddModuleAsync(client, "DeviceInput");
            gain = await factory.AddModuleAsync(client, "Gain");
            (
                await client.PutAsJsonAsync<ModuleDto>(
                    $"/api/modules/{gain.Id}",
                    ((ModuleDto<GainState>)gain) with
                    {
                        State = new GainState(-3f),
                    },
                    factory.Json
                )
            ).EnsureSuccessStatusCode();
            var response = await client.PostAsJsonAsync(
                "/api/connections",
                new CreateConnectionRequest(input.Id, "Output", gain.Id, "Input")
            );
            connection = await response.Content.ReadFromJsonAsync<ConnectionDto>();
        }

        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();
            var restored = await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{gain.Id}", factory.Json);
            var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

            await Assert.That(((ModuleDto<GainState>)restored!).State.Gain).IsEqualTo(-3f);
            await Assert.That(connections).IsEquivalentTo([connection!]);
        }
    }

    [Test]
    public async Task Configuration_RestoresChannelConnectionsAndShowsTheirChannels()
    {
        using var directory = new TempDirectory();
        ModuleDto source;
        ModuleDto target;
        ConnectionDto? connection;
        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();
            source = await factory.AddModuleAsync(client, "Gain");
            target = await factory.AddModuleAsync(client, "Gain");
            (
                await client.PutAsJsonAsync($"/api/modules/{target.Id}", target with { ChannelCount = 4 }, factory.Json)
            ).EnsureSuccessStatusCode();
            var response = await client.PostAsJsonAsync(
                "/api/connections",
                new CreateConnectionRequest(source.Id, "Output", target.Id, "Input", 1, 3)
            );
            connection = await response.Content.ReadFromJsonAsync<ConnectionDto>();
        }

        // a configuration written otherwise, e.g. by hand, gets the channels shown and the channel count the connections need
        var config = JsonNode.Parse(await File.ReadAllTextAsync(directory.Combine("config.json")))!;
        foreach (var module in config["modules"]!.AsArray())
        {
            module!["showChannels"] = false;
            module["channelCount"] = module["id"]!.GetValue<Guid>() == target.Id ? 2 : null;
        }

        await File.WriteAllTextAsync(directory.Combine("config.json"), config.ToJsonString());

        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();
            var modules = (await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json))!.ToDictionary(m =>
                m.Id
            );
            var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

            await Assert.That(connections).IsEquivalentTo([connection!]);
            await Assert.That(modules[source.Id].ShowChannels).IsTrue();
            await Assert.That(modules[target.Id].ShowChannels).IsTrue();
            await Assert.That(modules[target.Id].ChannelCount).IsEqualTo(4);
        }
    }

    [Test]
    public async Task Configuration_Unreadable_IsBackedUpAndEngineStartsEmpty()
    {
        using var directory = TempDirectory.Create();
        await File.WriteAllTextAsync(directory.Combine("config.json"), "{ not json");
        await using var factory = new EngineFactory(directory.Path);
        using var client = factory.CreateAuthorizedClient();

        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

        await Assert.That(modules).IsEmpty();
        await Assert.That(Directory.GetFiles(directory.Path, "config.json.*.bak")).HasSingleItem();
    }

    [Test]
    public async Task Configuration_WithEqualizerBandsWithoutType_LoadsThemAsPeaking()
    {
        using var directory = TempDirectory.Create();
        var id = Guid.NewGuid();
        await File.WriteAllTextAsync(
            directory.Combine("config.json"),
            $$"""
            {
              "version": 1,
              "modules": [
                { "type": "Equalizer", "id": "{{id}}", "state": { "bands": [{ "frequency": 100, "gain": 3, "q": 0.7 }] } }
              ]
            }
            """
        );
        await using var factory = new EngineFactory(directory.Path);
        using var client = factory.CreateAuthorizedClient();

        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

        var state = ((ModuleDto<EqualizerState>)modules!.Single(m => m.Id == id)).State;
        await Assert.That(state.Preamp).IsEqualTo(0f);
        await Assert.That(state.Bands).IsEquivalentTo([new EqualizerBand(100, 3f, 0.7f, EqualizerBandType.Peaking)]);
    }

    [Test]
    public async Task Configuration_KeepsUnknownModulesAndTheirConnectionsOutOfTheGraph()
    {
        using var directory = TempDirectory.Create();
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        var connection = Guid.NewGuid();
        await File.WriteAllTextAsync(
            directory.Combine("config.json"),
            $$"""
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
            """
        );
        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();

            var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);
            var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

            await Assert.That(modules!.Select(m => m.Id)).IsEquivalentTo([known]);
            await Assert.That(connections).IsEmpty();

            // saves the configuration
            var gain = (ModuleDto<GainState>)modules![0];
            (
                await client.PutAsJsonAsync<ModuleDto>(
                    $"/api/modules/{known}",
                    gain with
                    {
                        State = new GainState(-3f),
                    },
                    factory.Json
                )
            ).EnsureSuccessStatusCode();
        }

        var saved = JsonNode.Parse(await File.ReadAllTextAsync(directory.Combine("config.json")))!;
        var savedModules = saved["modules"]!.AsArray();

        await Assert.That(savedModules.Select(m => (string)m!["type"]!)).IsEquivalentTo(["Gain", "Removed"]);
        await Assert
            .That(saved["connections"]!.AsArray().Select(c => (string)c!["id"]!))
            .IsEquivalentTo([connection.ToString()]);
    }

    [Test]
    public async Task Configuration_ChannelConnectionsOfUnknownModules_DontLimitTheChannelSettings()
    {
        using var directory = TempDirectory.Create();
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        await File.WriteAllTextAsync(
            directory.Combine("config.json"),
            $$"""
            {
              "version": 1,
              "modules": [
                { "type": "Removed", "id": "{{unknown}}", "state": {} },
                { "type": "Gain", "id": "{{known}}", "showChannels": true, "channelCount": 6, "state": { "gain": 0 } }
              ],
              "connections": [
                {
                  "id": "{{Guid.NewGuid()}}", "sourceModuleId": "{{unknown}}", "sourcePort": "Output", "targetModuleId": "{{known}}",
                  "targetPort": "Input", "targetChannel": 5
                }
              ]
            }
            """
        );
        await using var factory = new EngineFactory(directory.Path);
        using var client = factory.CreateAuthorizedClient();
        var gain = (await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{known}", factory.Json))!;

        using var response = await client.PutAsJsonAsync(
            $"/api/modules/{known}",
            gain with
            {
                ShowChannels = false,
                ChannelCount = 2,
            },
            factory.Json
        );
        var updated = await response.Content.ReadFromJsonAsync<ModuleDto>(factory.Json);

        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(updated!.ShowChannels).IsFalse();
        await Assert.That(updated.ChannelCount).IsEqualTo(2);
    }
}
