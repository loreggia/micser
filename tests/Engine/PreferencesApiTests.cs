using System.Net.Http.Json;
using Micser.Engine.Contracts;

namespace Micser.Engine.Tests;

public class PreferencesApiTests
{
    [Test]
    public async Task GetPreferences_ReturnsDefaults()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var preferences = await client.GetFromJsonAsync<UiPreferencesDto>("/api/preferences");

        await Assert.That(preferences).IsEqualTo(new UiPreferencesDto(ShowStreamStatistics: false, SnapToGrid: true));
    }

    [Test]
    public async Task UpdatePreferences_BroadcastsAndPersists()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        var updated = new UiPreferencesDto(ShowStreamStatistics: true, SnapToGrid: false, Language: "de");
        try
        {
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                await using var hub = factory.CreateHubConnection();
                var changed = hub.NextAsync<UiPreferencesDto>("PreferencesChanged");
                await hub.StartAsync();

                (await client.PutAsJsonAsync("/api/preferences", updated)).EnsureSuccessStatusCode();

                await Assert.That(await changed).IsEqualTo(updated);
            }

            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();

                await Assert.That(await client.GetFromJsonAsync<UiPreferencesDto>("/api/preferences")).IsEqualTo(updated);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
