using System.Net;
using System.Net.Http.Json;
using System.Text;
using Micser.Engine.Contracts;
using Micser.Plugins.Main.Modules;

namespace Micser.Engine.Tests;

public class ConfigurationApiTests
{
    [Test]
    public async Task ExportThenImport_ReplacesTheConfigurationAndBroadcasts()
    {
        var preferences = new UiPreferencesDto(ShowStreamStatistics: true, SnapToGrid: false, Language: "de");
        byte[] exported;
        ModuleDto[] modules;
        ConnectionDto[] connections;
        SubgraphDto[] subgraphs;
        await using (var source = new EngineFactory())
        {
            using var client = source.CreateAuthorizedClient();
            var input = await source.AddModuleAsync(client, "DeviceInput");
            var gain = await source.AddModuleAsync(client, "Gain");
            (
                await client.PutAsJsonAsync<ModuleDto>(
                    $"/api/modules/{gain.Id}",
                    ((ModuleDto<GainState>)gain) with
                    {
                        State = new GainState(-3f),
                    },
                    source.Json
                )
            ).EnsureSuccessStatusCode();
            (
                await client.PostAsJsonAsync(
                    "/api/connections",
                    new CreateConnectionRequest(input.Id, "Output", gain.Id, "Input")
                )
            ).EnsureSuccessStatusCode();
            (
                await client.PostAsJsonAsync(
                    "/api/subgraphs",
                    new CreateSubgraphRequest("Chain", new ModulePosition(0, 0), new SubgraphSize(400, 300), [gain.Id])
                )
            ).EnsureSuccessStatusCode();
            (await client.PutAsJsonAsync("/api/preferences", preferences)).EnsureSuccessStatusCode();

            exported = await client.GetByteArrayAsync("/api/configuration");
            modules = (await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", source.Json))!;
            connections = (await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections"))!;
            subgraphs = (await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", source.Json))!;
        }

        await using var target = new EngineFactory();
        using var targetClient = target.CreateAuthorizedClient();
        var replaced = await target.AddModuleAsync(targetClient, "Gain");
        await using var hub = target.CreateHubConnection();
        var removed = hub.NextAsync<Guid>("ModuleRemoved");
        var preferencesChanged = hub.NextAsync<UiPreferencesDto>("PreferencesChanged");
        await hub.StartReceivingAsync();

        var response = await targetClient.PutAsync("/api/configuration", CreateUpload(exported));

        response.EnsureSuccessStatusCode();
        await Assert.That(await removed).IsEqualTo(replaced.Id);
        await Assert.That(await preferencesChanged).IsEqualTo(preferences);
        await Assert
            .That(await targetClient.GetFromJsonAsync<ModuleDto[]>("/api/modules", target.Json))
            .IsEquivalentTo(modules);
        await Assert
            .That(await targetClient.GetFromJsonAsync<ConnectionDto[]>("/api/connections"))
            .IsEquivalentTo(connections);
        await Assert
            .That(await targetClient.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", target.Json))
            .IsEquivalentTo(subgraphs);
        await Assert.That(Directory.GetFiles(target.Directory, "config.json.*.bak")).HasSingleItem();
    }

    [Test]
    public async Task Import_KeepsBuiltInTemplatesAndReplacesTheUsersTemplates()
    {
        await using var factory = new EngineFactory(pluginTemplates: true);
        using var client = factory.CreateAuthorizedClient();
        var builtIn = (await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json))!;
        var gain = await factory.AddModuleAsync(client, "Gain");
        var subgraph = await (
            await client.PostAsJsonAsync(
                "/api/subgraphs",
                new CreateSubgraphRequest("Chain", new ModulePosition(0, 0), new SubgraphSize(400, 300), [gain.Id])
            )
        ).Content.ReadFromJsonAsync<SubgraphDto>(factory.Json);
        (
            await client.PostAsJsonAsync(
                "/api/subgraph-templates",
                new SaveSubgraphTemplateRequest(subgraph!.Id, "Mine")
            )
        ).EnsureSuccessStatusCode();

        var response = await client.PutAsync(
            "/api/configuration",
            CreateUpload( /*lang=json,strict*/
                """{ "version": 1 }"""u8.ToArray()
            )
        );

        response.EnsureSuccessStatusCode();
        var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);
        await Assert.That(builtIn).IsNotEmpty();
        await Assert.That(templates!.Select(t => t.Id)).IsEquivalentTo(builtIn.Select(t => t.Id));
        await Assert.That(await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json)).IsEmpty();
    }

    [Test]
    [Arguments("{ not json")]
    [Arguments( /*lang=json,strict*/
        """{ "version": 99 }"""
    )]
    [Arguments("[]")]
    public async Task Import_InvalidFile_IsRejectedAndKeepsTheConfiguration(string content)
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var gain = await factory.AddModuleAsync(client, "Gain");

        var response = await client.PutAsync("/api/configuration", CreateUpload(Encoding.UTF8.GetBytes(content)));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);
        await Assert.That(modules!.Select(m => m.Id)).IsEquivalentTo([gain.Id]);
    }

    private static MultipartFormDataContent CreateUpload(byte[] content)
    {
        return new MultipartFormDataContent { { new ByteArrayContent(content), "file", "config.json" } };
    }
}
