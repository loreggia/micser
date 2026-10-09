using System.Net.Http.Json;
using Micser.Audio.Devices;
using Micser.Engine.Contracts;

namespace Micser.Engine.Tests;

public class SystemVolumeTests
{
    [Test]
    public async Task UseSystemVolume_TakesVolumeAndMuteFromTheSystem()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        factory.SystemVolume.Set(new SystemVolumeLevel(0.5f, true));
        var module = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PutAsJsonAsync(
            $"/api/modules/{module.Id}",
            module with
            {
                UseSystemVolume = true,
                Volume = 0.2f,
                IsMuted = false,
            },
            factory.Json
        );
        var updated = await response.Content.ReadFromJsonAsync<ModuleDto>(factory.Json);

        await Assert.That(updated!.UseSystemVolume).IsTrue();
        await Assert.That(updated.Volume).IsEqualTo(0.5f);
        await Assert.That(updated.IsMuted).IsTrue();
    }

    [Test]
    public async Task SystemVolumeChange_UpdatesFollowingModulesOnly()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var following = await factory.AddModuleAsync(client, "Gain");
        var other = await factory.AddModuleAsync(client, "Gain");
        (
            await client.PutAsJsonAsync(
                $"/api/modules/{following.Id}",
                following with
                {
                    UseSystemVolume = true,
                },
                factory.Json
            )
        ).EnsureSuccessStatusCode();
        await using var hub = factory.CreateHubConnection();
        // the change from the PUT above may still be queued for sending when the hub connects
        var changed = hub.NextAsync<ModuleDto>("ModuleChanged", m => m.Id == following.Id && m.Volume == 0.25f);
        await hub.StartAsync();

        factory.SystemVolume.Set(new SystemVolumeLevel(0.25f, false));
        var pushed = await changed;
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

        await Assert.That(pushed.IsMuted).IsFalse();
        await Assert.That(modules!.Single(m => m.Id == following.Id).Volume).IsEqualTo(0.25f);
        await Assert.That(modules!.Single(m => m.Id == other.Id).Volume).IsEqualTo(1f);
    }
}
