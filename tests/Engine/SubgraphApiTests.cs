using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Micser.Audio;
using Micser.Engine.Audio;
using Micser.Engine.Contracts;

namespace Micser.Engine.Tests;

public class SubgraphApiTests
{
    [Test]
    public async Task AddModule_UnknownSubgraph_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/modules",
            new CreateModuleRequest("Gain", SubgraphId: Guid.NewGuid()),
            factory.Json
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateSubgraph_MovesModulesWithRelativePositions()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var gain = await AddModuleAtAsync(factory, client, "Gain", new ModulePosition(100, 200));
        var other = await AddModuleAtAsync(factory, client, "Gain", new ModulePosition(500, 500));

        var subgraph = await CreateSubgraphAsync(factory, client, new ModulePosition(80, 150), gain.Id);
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);
        var subgraphs = await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json);

        var moved = modules!.Single(m => m.Id == gain.Id);
        await Assert.That(moved.SubgraphId).IsEqualTo(subgraph.Id);
        await Assert.That(moved.Position).IsEqualTo(new ModulePosition(20, 50));
        await Assert.That(modules!.Single(m => m.Id == other.Id).SubgraphId).IsNull();
        await Assert.That(subgraphs).IsEquivalentTo([subgraph]);
    }

    [Test]
    public async Task CreateSubgraph_FromAnotherSubgraph_KeepsAbsolutePosition()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var gain = await AddModuleAtAsync(factory, client, "Gain", new ModulePosition(100, 200));
        await CreateSubgraphAsync(factory, client, new ModulePosition(80, 150), gain.Id);

        var second = await CreateSubgraphAsync(factory, client, new ModulePosition(0, 0), gain.Id);
        var moved = await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{gain.Id}", factory.Json);

        await Assert.That(moved!.SubgraphId).IsEqualTo(second.Id);
        await Assert.That(moved.Position).IsEqualTo(new ModulePosition(100, 200));
    }

    [Test]
    public async Task CreateSubgraph_UnknownModule_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/subgraphs",
            new CreateSubgraphRequest(null, new ModulePosition(0, 0), new SubgraphSize(100, 100), [Guid.NewGuid()]),
            factory.Json
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteSubgraph_KeepsModulesWithAbsolutePositions()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var gain = await AddModuleAtAsync(factory, client, "Gain", new ModulePosition(100, 200));
        var subgraph = await CreateSubgraphAsync(factory, client, new ModulePosition(80, 150), gain.Id);
        (
            await client.PutAsJsonAsync(
                $"/api/subgraphs/{subgraph.Id}",
                subgraph with
                {
                    Position = new ModulePosition(0, 0),
                    IsMuted = true,
                },
                factory.Json
            )
        ).EnsureSuccessStatusCode();
        await using var hub = factory.CreateHubConnection();
        var removed = hub.NextAsync<Guid>("SubgraphRemoved");
        await hub.StartAsync();

        using var response = await client.DeleteAsync($"/api/subgraphs/{subgraph.Id}");
        var module = await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{gain.Id}", factory.Json);
        using var missing = await client.DeleteAsync($"/api/subgraphs/{subgraph.Id}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(await removed).IsEqualTo(subgraph.Id);
        await Assert.That(module!.SubgraphId).IsNull();
        await Assert.That(module.Position).IsEqualTo(new ModulePosition(20, 50));
        await Assert.That(GetAudioModule(factory, gain.Id).IsMuted).IsFalse();
        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteSubgraph_WithModules_RemovesThemAndTheirConnections()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var member = await AddModuleAtAsync(factory, client, "Gain", new ModulePosition(100, 200));
        var outside = await AddModuleAtAsync(factory, client, "Gain", new ModulePosition(600, 200));
        (
            await client.PostAsJsonAsync(
                "/api/connections",
                new CreateConnectionRequest(member.Id, "Output", outside.Id, "Input")
            )
        ).EnsureSuccessStatusCode();
        var subgraph = await CreateSubgraphAsync(factory, client, new ModulePosition(80, 150), member.Id);
        await using var hub = factory.CreateHubConnection();
        var moduleRemoved = hub.NextAsync<Guid>("ModuleRemoved");
        await hub.StartAsync();

        using var response = await client.DeleteAsync($"/api/subgraphs/{subgraph.Id}?deleteModules=true");
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);
        var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");
        var subgraphs = await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(await moduleRemoved).IsEqualTo(member.Id);
        await Assert.That(modules!.Select(m => m.Id)).IsEquivalentTo([outside.Id]);
        await Assert.That(connections).IsEmpty();
        await Assert.That(subgraphs).IsEmpty();
    }

    [Test]
    public async Task Subgraphs_AreRestoredAfterRestart()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        try
        {
            ModuleDto gain;
            SubgraphDto subgraph;
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                gain = await AddModuleAtAsync(factory, client, "Gain", new ModulePosition(100, 200));
                subgraph = await CreateSubgraphAsync(factory, client, new ModulePosition(80, 150), gain.Id);
                subgraph = subgraph with
                {
                    Name = "Mic chain",
                    Color = SubgraphColor.Teal,
                    IsCollapsed = true,
                    IsMuted = true,
                };
                (
                    await client.PutAsJsonAsync($"/api/subgraphs/{subgraph.Id}", subgraph, factory.Json)
                ).EnsureSuccessStatusCode();
            }

            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var subgraphs = await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json);
                var module = await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{gain.Id}", factory.Json);

                await Assert.That(subgraphs).IsEquivalentTo([subgraph]);
                await Assert.That(module!.SubgraphId).IsEqualTo(subgraph.Id);
                await Assert.That(GetAudioModule(factory, gain.Id).IsMuted).IsTrue();
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task UpdateModule_UnknownSubgraph_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var gain = await factory.AddModuleAsync(client, "Gain");

        using var response = await client.PutAsJsonAsync(
            $"/api/modules/{gain.Id}",
            gain with
            {
                SubgraphId = Guid.NewGuid(),
            },
            factory.Json
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateSubgraph_MuteAndBypassCombineWithTheModules()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var gain = await factory.AddModuleAsync(client, "Gain");
        var subgraph = await CreateSubgraphAsync(factory, client, new ModulePosition(0, 0), gain.Id);
        await using var hub = factory.CreateHubConnection();
        // the change from creating the subgraph may still be queued for sending when the hub connects
        var changed = hub.NextAsync<SubgraphDto>("SubgraphChanged", s => s.IsMuted);
        await hub.StartAsync();

        using var response = await client.PutAsJsonAsync(
            $"/api/subgraphs/{subgraph.Id}",
            subgraph with
            {
                IsMuted = true,
                IsBypassed = true,
            },
            factory.Json
        );
        var module = await client.GetFromJsonAsync<ModuleDto>($"/api/modules/{gain.Id}", factory.Json);
        var audioModule = (EffectModule)GetAudioModule(factory, gain.Id);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That((await changed).IsBypassed).IsTrue();
        await Assert.That(module!.IsMuted).IsFalse();
        await Assert.That(module.IsBypassed).IsFalse();
        await Assert.That(audioModule.IsMuted).IsTrue();
        await Assert.That(audioModule.IsBypassed).IsTrue();

        (
            await client.PutAsJsonAsync($"/api/subgraphs/{subgraph.Id}", subgraph, factory.Json)
        ).EnsureSuccessStatusCode();

        await Assert.That(audioModule.IsMuted).IsFalse();
        await Assert.That(audioModule.IsBypassed).IsFalse();
    }

    private static async Task<ModuleDto> AddModuleAtAsync(
        EngineFactory factory,
        HttpClient client,
        string type,
        ModulePosition position
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/modules",
            new CreateModuleRequest(type, Position: position),
            factory.Json
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ModuleDto>(factory.Json))!;
    }

    private static async Task<SubgraphDto> CreateSubgraphAsync(
        EngineFactory factory,
        HttpClient client,
        ModulePosition position,
        params Guid[] moduleIds
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/subgraphs",
            new CreateSubgraphRequest(null, position, new SubgraphSize(400, 300), moduleIds),
            factory.Json
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubgraphDto>(factory.Json))!;
    }

    private static AudioModule GetAudioModule(EngineFactory factory, Guid id)
    {
        return factory.Services.GetRequiredService<AudioHost>().GetAudioModule(id)!;
    }
}
