using System.Net.Http.Json;
using Micser.Engine.Contracts;
using Micser.Tests.Common;

namespace Micser.Engine.Tests;

public class PreferencesApiTests
{
    [Test]
    public async Task GetPreferences_ReturnsDefaults()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var preferences = await client.GetFromJsonAsync<UiPreferencesDto>("/api/preferences", factory.Json);

        await Assert.That(preferences).IsEqualTo(new UiPreferencesDto(ShowStreamStatistics: false, SnapToGrid: true));
    }

    [Test]
    public async Task UpdatePreferences_BroadcastsAndPersists()
    {
        using var directory = new TempDirectory();
        var updated = new UiPreferencesDto(
            ShowStreamStatistics: true,
            SnapToGrid: false,
            Language: "de",
            ShowChannelsByDefault: true,
            Theme: new UiThemeDto("HighContrast", UiColorMode.Dark),
            ShowHelp: false
        );
        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();
            await using var hub = factory.CreateHubConnection();
            var changed = hub.NextAsync<UiPreferencesDto>("PreferencesChanged");
            await hub.StartReceivingAsync();

            (await client.PutAsJsonAsync("/api/preferences", updated, factory.Json)).EnsureSuccessStatusCode();

            await Assert.That(await changed).IsEqualTo(updated);
        }

        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();

            await Assert
                .That(await client.GetFromJsonAsync<UiPreferencesDto>("/api/preferences", factory.Json))
                .IsEqualTo(updated);
        }
    }
}
