using System.Net;
using System.Net.Http.Json;
using Micser.Engine.Contracts;

namespace Micser.Engine.Tests;

public class ConnectionApiTests
{
    [Test]
    public async Task Connect_ChannelsMakeConnectionsDistinct()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");

        using var whole = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input"));
        using var channel = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input", 1, 0));
        using var duplicate = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input", 1, 0));
        var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

        await Assert.That(whole.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(channel.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(duplicate.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(connections!.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Connect_Channels_TurnShowChannelsOnWhereTheyAreNamed()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");
        var third = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input", SourceChannel: 1));
        var connection = await response.Content.ReadFromJsonAsync<ConnectionDto>();
        (await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(second.Id, "Output", third.Id, "Input", TargetChannel: 0))).EnsureSuccessStatusCode();
        var modules = (await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json))!.ToDictionary(m => m.Id);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(connection!.SourceChannel).IsEqualTo(1);
        await Assert.That(connection.TargetChannel).IsNull();
        await Assert.That(modules[first.Id].ShowChannels).IsTrue();
        await Assert.That(modules[second.Id].ShowChannels).IsFalse();
        await Assert.That(modules[third.Id].ShowChannels).IsTrue();
    }

    [Test]
    public async Task Connect_CreatesAndListsConnection()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var source = await factory.AddModuleAsync(client, "DeviceInput");
        var target = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(source.Id, "Output", target.Id, "Input"));
        var connection = await response.Content.ReadFromJsonAsync<ConnectionDto>();
        var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(connections).IsEquivalentTo([connection!]);
    }

    [Test]
    public async Task Connect_Cycle_IsConflict()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");
        (await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input"))).EnsureSuccessStatusCode();

        using var cycle = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(second.Id, "Output", first.Id, "Input"));
        using var duplicate = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input"));

        await Assert.That(cycle.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(duplicate.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    [Arguments(-1, null)]
    [Arguments(64, null)]
    [Arguments(null, 64)]
    public async Task Connect_InvalidChannel_IsBadRequest(int? sourceChannel, int? targetChannel)
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PostAsJsonAsync(
            "/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input", sourceChannel, targetChannel));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Connect_TargetChannelBeyondTheChannelCount_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var source = await factory.AddModuleAsync(client, "Gain");
        var target = await factory.AddModuleAsync(client, "Gain");
        var device = await factory.AddModuleAsync(client, "DeviceOutput");
        (await client.PutAsJsonAsync($"/api/modules/{target.Id}", target with { ChannelCount = 2 }, factory.Json)).EnsureSuccessStatusCode();

        using var beyond = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(source.Id, "Output", target.Id, "Input", TargetChannel: 2));
        using var within = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(source.Id, "Output", target.Id, "Input", TargetChannel: 1));
        using var toDevice = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(source.Id, "Output", device.Id, "Input", TargetChannel: 7));

        await Assert.That(beyond.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(within.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(toDevice.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    [Arguments("Nope", "Input")]
    [Arguments("Output", "Nope")]
    public async Task Connect_UnknownPort_IsBadRequest(string sourcePort, string targetPort)
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, sourcePort, second.Id, targetPort));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Disconnect_RemovesConnection()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var first = await factory.AddModuleAsync(client, "Gain");
        var second = await factory.AddModuleAsync(client, "Gain");
        var created = await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input"));
        var connection = await created.Content.ReadFromJsonAsync<ConnectionDto>();

        using var response = await client.DeleteAsync($"/api/connections/{connection!.Id}");
        using var missing = await client.DeleteAsync($"/api/connections/{connection.Id}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
