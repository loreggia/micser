using System.Net;
using System.Net.Http.Json;
using Micser.Engine.Contracts;

namespace Micser.Engine.Tests;

public class ConnectionApiTests
{
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
