using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Micser.Audio;
using Micser.Engine.Contracts;
using Micser.Tests.Common;

namespace Micser.Engine.Tests;

public class CablesApiTests
{
    [Test]
    public async Task GetCables_IsEmptyAtFirst()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var cables = await client.GetFromJsonAsync<VirtualCablesDto>("/api/cables", factory.Json);

        await Assert.That(cables!.IsSupported).IsTrue();
        await Assert.That(cables.Cables).IsEmpty();
    }

    [Test]
    public async Task UpdateCables_AppliesBroadcastsAndPersists()
    {
        using var directory = new TempDirectory();
        CableDto[] cables = [new(CableLayout.Stereo), new(CableLayout.Surround71)];
        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();
            await using var hub = factory.CreateHubConnection();
            var changed = hub.NextAsync<VirtualCablesDto>("CablesChanged");
            await hub.StartAsync();

            var response = await client.PutAsJsonAsync("/api/cables", new UpdateCablesRequest(cables), factory.Json);
            response.EnsureSuccessStatusCode();

            await Assert
                .That((await response.Content.ReadFromJsonAsync<VirtualCablesDto>(factory.Json))!.Cables)
                .IsEquivalentTo(cables);
            await Assert.That((await changed).Cables).IsEquivalentTo(cables);
            await Assert
                .That(factory.VirtualCables.Layouts)
                .IsEquivalentTo((ChannelLayout[])[ChannelLayout.Stereo, ChannelLayout.Surround71]);
        }

        // the next start creates them again
        await using (var factory = new EngineFactory(directory.Path))
        {
            using var client = factory.CreateAuthorizedClient();

            var restored = await client.GetFromJsonAsync<VirtualCablesDto>("/api/cables", factory.Json);

            await Assert.That(restored!.Cables).IsEquivalentTo(cables);
            await Assert
                .That(factory.VirtualCables.Layouts)
                .IsEquivalentTo((ChannelLayout[])[ChannelLayout.Stereo, ChannelLayout.Surround71]);
        }
    }

    [Test]
    public async Task UpdateCables_RejectsMoreThan16()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        var response = await client.PutAsJsonAsync(
            "/api/cables",
            new UpdateCablesRequest([.. Enumerable.Repeat(new CableDto(CableLayout.Stereo), 17)]),
            factory.Json
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        await Assert.That(problem!.Errors.Keys).Contains("cables");
        // only the start applied cables, none
        await Assert.That(factory.VirtualCables.Layouts).IsEmpty();
    }

    [Test]
    public async Task UpdateCables_IsRejectedWhereTheEngineDoesNotCreateCables()
    {
        await using var factory = new EngineFactory();
        factory.VirtualCables.IsSupported = false;
        using var client = factory.CreateAuthorizedClient();

        var cables = await client.GetFromJsonAsync<VirtualCablesDto>("/api/cables", factory.Json);
        var response = await client.PutAsJsonAsync(
            "/api/cables",
            new UpdateCablesRequest([new CableDto(CableLayout.Stereo)]),
            factory.Json
        );

        await Assert.That(cables!.IsSupported).IsFalse();
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ImportConfiguration_ReplacesTheCables()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        (
            await client.PutAsJsonAsync(
                "/api/cables",
                new UpdateCablesRequest([new CableDto(CableLayout.Stereo), new CableDto(CableLayout.Stereo)]),
                factory.Json
            )
        ).EnsureSuccessStatusCode();
        await using var hub = factory.CreateHubConnection();
        var changed = hub.NextAsync<VirtualCablesDto>("CablesChanged");
        await hub.StartAsync();

        var content = /*lang=json,strict*/
            """{ "version": 1, "modules": [], "cables": [ { "layout": "Surround51" } ] }""";
        var upload = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", "config.json" },
        };
        (await client.PutAsync("/api/configuration", upload)).EnsureSuccessStatusCode();

        await Assert.That((await changed).Cables).IsEquivalentTo((CableDto[])[new(CableLayout.Surround51)]);
        await Assert.That(factory.VirtualCables.Layouts).IsEquivalentTo((ChannelLayout[])[ChannelLayout.Surround51]);
    }
}
