using System.Net.Http.Json;
using System.Text.Json;
using Micser.Engine.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace Micser.Engine.Tests;

public class EngineHubTests
{
    [Test]
    public async Task Changes_AreBroadcastInOrder()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        await using var hub = factory.CreateHubConnection();
        var events = new List<string>();
        hub.On<ModuleDto>("ModuleChanged", m => { lock (events) events.Add($"module {m.Id}"); });
        hub.On<ConnectionDto>("ConnectionAdded", c => { lock (events) events.Add($"connection {c.Id}"); });
        var removed = hub.NextAsync<Guid>("ModuleRemoved");
        await hub.StartAsync();

        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");
        var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input"));
        var connection = await response.Content.ReadFromJsonAsync<ConnectionDto>();
        await client.DeleteAsync($"/api/modules/{first.Id}");

        await Assert.That(await removed).IsEqualTo(first.Id);
        string[] received;
        lock (events)
        {
            received = [.. events];
        }

        await Assert.That(received).IsEquivalentTo(
            [$"module {first.Id}", $"module {second.Id}", $"connection {connection!.Id}"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Subscribe_DeliversModuleData()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var spectrum = await factory.AddModuleAsync(client, "Spectrum");
        await using var hub = factory.CreateHubConnection();
        var data = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<Guid, JsonElement>("ModuleData", (id, value) =>
        {
            if (id == spectrum.Id)
            {
                data.TrySetResult(value);
            }
        });
        await hub.StartAsync();

        await hub.InvokeAsync("Subscribe", spectrum.Id);
        var value = await data.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await Assert.That(value.GetProperty("magnitudes").GetArrayLength()).IsEqualTo(2049);
    }

    [Test]
    public async Task RestartAudio_KeepsModulesAndBroadcastsStatus()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var module = await factory.AddModuleAsync(client, "Gain");
        await using var hub = factory.CreateHubConnection();
        var status = hub.NextAsync<EngineStatusDto>("StatusChanged");
        await hub.StartAsync();

        using var response = await client.PostAsync("/api/engine/restart-audio", null);
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That((await status).IsRunning).IsTrue();
        await Assert.That(modules!.Select(m => m.Id)).IsEquivalentTo([module.Id]);
    }

    [Test]
    public async Task SubscribeLevels_DeliversLevels()
    {
        await using var factory = new EngineFactory();
        await using var hub = factory.CreateHubConnection();
        var levels = hub.NextAsync<Dictionary<Guid, PortLevelsDto[]>>("Levels");
        await hub.StartAsync();

        await hub.InvokeAsync("SubscribeLevels");

        // without devices no module produces a signal, so there are no levels to report
        await Assert.That(await levels.WaitAsync(TimeSpan.FromSeconds(10))).IsEmpty();
    }

    [Test]
    public async Task UpdateSettings_RebuildsGraphAndBroadcastsStatus()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var module = await factory.AddModuleAsync(client, "Gain");
        await using var hub = factory.CreateHubConnection();
        var status = hub.NextAsync<EngineStatusDto>("StatusChanged");
        await hub.StartAsync();

        using var response = await client.PutAsJsonAsync("/api/engine/settings", new EngineSettingsDto(44100, 128));
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

        await Assert.That((await status).Settings).IsEqualTo(new EngineSettingsDto(44100, 128));
        await Assert.That((await status).IsRunning).IsTrue();
        await Assert.That(modules!.Select(m => m.Id)).IsEquivalentTo([module.Id]);
    }
}
