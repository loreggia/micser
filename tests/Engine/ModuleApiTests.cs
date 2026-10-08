using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Micser.Engine.Audio;
using Micser.Engine.Contracts;
using Micser.Plugins.Main.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.Engine.Tests;

public class ModuleApiTests
{
    [Test]
    public async Task AddModule_UnknownType_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        using var response = await client.PostAsJsonAsync("/api/modules", new CreateModuleRequest("Nope"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AddModule_UsesDefaultStateAndIsListed()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var module = await factory.AddModuleAsync(client, "Compressor");
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

        await Assert.That(module).IsTypeOf<ModuleDto<CompressorState>>();
        await Assert.That(((ModuleDto<CompressorState>)module).State).IsEqualTo(new CompressorState());
        await Assert.That(modules!.Select(m => m.Id)).Contains(module.Id);
    }

    [Test]
    public async Task ModuleTypes_ListPortsAndDefaultStates()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var types = await client.GetFromJsonAsync<JsonArray>("/api/module-types");
        var gain = types!.Single(t => (string?)t!["type"] == "Gain")!;

        await Assert.That(types!.Select(t => (string)t!["type"]!)).IsEquivalentTo(
            ["DeviceInput", "LoopbackInput", "DeviceOutput", "Gain", "Compressor", "Equalizer", "Pitch", "Spectrum"]);
        await Assert.That(gain["inputs"]!.AsArray().Select(p => (string)p!)).IsEquivalentTo(["Input"]);
        await Assert.That((float?)gain["defaultState"]!["gain"]).IsEqualTo(0f);
        await Assert.That((bool?)gain["supportsBypass"]).IsTrue();
        await Assert.That((bool?)types!.Single(t => (string?)t!["type"] == "DeviceOutput")!["supportsBypass"]).IsFalse();
    }

    [Test]
    public async Task ModuleTypes_TellWhichSupportAChannelCount()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var types = await client.GetFromJsonAsync<ModuleTypeDto[]>("/api/module-types");
        var supported = types!.Where(t => t.SupportsChannelCount).Select(t => t.Type);

        await Assert.That(supported).IsEquivalentTo(["Gain", "Compressor", "Equalizer", "Pitch", "Spectrum"]);
    }

    [Test]
    public async Task RemoveModule_RemovesItAndItsConnections()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");
        (await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input"))).EnsureSuccessStatusCode();

        using var response = await client.DeleteAsync($"/api/modules/{first.Id}");
        var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");
        using var missing = await client.DeleteAsync($"/api/modules/{first.Id}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(connections).IsEmpty();
        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateModule_AppliesSettingsAndState()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var module = (ModuleDto<GainState>)await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PutAsJsonAsync<ModuleDto>(
            $"/api/modules/{module.Id}",
            module with { Name = "Boost", Volume = 0.5f, IsBypassed = true, IsCollapsed = true, State = new GainState(6f), Position = new ModulePosition(10, 20) },
            factory.Json);
        var updated = (ModuleDto<GainState>)(await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{module.Id}", factory.Json))!;

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(updated.State.Gain).IsEqualTo(6f);
        await Assert.That(updated.Volume).IsEqualTo(0.5f);
        await Assert.That(updated.IsBypassed).IsTrue();
        await Assert.That(updated.IsCollapsed).IsTrue();
        await Assert.That(updated.Name).IsEqualTo("Boost");
        await Assert.That(updated.Position).IsEqualTo(new ModulePosition(10, 20));
    }

    [Test]
    public async Task UpdateModule_BypassOnNonEffect_IsIgnored()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var module = (ModuleDto<DeviceOutputState>)await factory.AddModuleAsync(client, "DeviceOutput");

        using var response = await client.PutAsJsonAsync<ModuleDto>($"/api/modules/{module.Id}", module with { IsBypassed = true }, factory.Json);
        var updated = await response.Content.ReadFromJsonAsync<ModuleDto>(factory.Json);

        await Assert.That(updated!.IsBypassed).IsFalse();
    }

    [Test]
    public async Task UpdateModule_ChannelCount_AppliesToModulesThatSupportIt()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var gain = await factory.AddModuleAsync(client, "Gain");
        var device = await factory.AddModuleAsync(client, "DeviceOutput");

        using var gainResponse = await client.PutAsJsonAsync($"/api/modules/{gain.Id}", gain with { ChannelCount = 6 }, factory.Json);
        using var deviceResponse = await client.PutAsJsonAsync($"/api/modules/{device.Id}", device with { ChannelCount = 6 }, factory.Json);
        var updatedGain = await gainResponse.Content.ReadFromJsonAsync<ModuleDto>(factory.Json);
        var updatedDevice = await deviceResponse.Content.ReadFromJsonAsync<ModuleDto>(factory.Json);

        await Assert.That(updatedGain!.ChannelCount).IsEqualTo(6);
        await Assert.That(factory.Services.GetRequiredService<AudioHost>().GetAudioModule(gain.Id)!.ChannelCount).IsEqualTo(6);
        await Assert.That(updatedDevice!.ChannelCount).IsNull();
    }

    [Test]
    public async Task UpdateModule_ChannelSettingsInUse_AreBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var source = await factory.AddModuleAsync(client, "Gain");
        var target = await factory.AddModuleAsync(client, "Gain");
        var created = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(source.Id, "Output", target.Id, "Input", TargetChannel: 3));
        var connection = await created.Content.ReadFromJsonAsync<ConnectionDto>();
        target = (await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{target.Id}", factory.Json))!;

        using var hidden = await client.PutAsJsonAsync($"/api/modules/{target.Id}", target with { ShowChannels = false }, factory.Json);
        using var tooFew = await client.PutAsJsonAsync($"/api/modules/{target.Id}", target with { ChannelCount = 3 }, factory.Json);
        using var enough = await client.PutAsJsonAsync($"/api/modules/{target.Id}", target with { ChannelCount = 4 }, factory.Json);
        (await client.DeleteAsync($"/api/connections/{connection!.Id}")).EnsureSuccessStatusCode();
        using var hiddenWithoutConnections = await client.PutAsJsonAsync($"/api/modules/{target.Id}", target with { ShowChannels = false }, factory.Json);
        var hiddenErrors = (await hidden.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject().Select(e => e.Key);
        var tooFewErrors = (await tooFew.Content.ReadFromJsonAsync<JsonObject>())!["errors"]!.AsObject().Select(e => e.Key);

        await Assert.That(target.ShowChannels).IsTrue();
        await Assert.That(hidden.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(hiddenErrors).IsEquivalentTo(["showChannels"]);
        await Assert.That(tooFew.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(tooFewErrors).IsEquivalentTo(["channelCount"]);
        await Assert.That(enough.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(hiddenWithoutConnections.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UpdateModule_InvalidState_ReturnsErrorsByPath()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var module = (ModuleDto<EqualizerState>)await factory.AddModuleAsync(client, "Equalizer");

        using var response = await client.PutAsJsonAsync<ModuleDto>(
            $"/api/modules/{module.Id}",
            module with { Volume = 2f, State = new EqualizerState([new EqualizerBand(1000, 0), new EqualizerBand(5, 0)]) },
            factory.Json);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        var errors = problem!["errors"]!.AsObject().Select(e => e.Key).ToArray();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(errors).IsEquivalentTo(["volume", "state.bands[1].frequency"]);
    }

    [Test]
    public async Task UpdateModule_NameTooLong_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var module = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PutAsJsonAsync($"/api/modules/{module.Id}", module with { Name = new string('x', 101) }, factory.Json);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(problem!["errors"]!.AsObject().Select(e => e.Key)).IsEquivalentTo(["name"]);
    }

    [Test]
    public async Task UpdateModule_OtherType_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var module = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PutAsJsonAsync<ModuleDto>(
            $"/api/modules/{module.Id}",
            new ModuleDto<PitchState> { Id = module.Id, State = new PitchState() },
            factory.Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}
