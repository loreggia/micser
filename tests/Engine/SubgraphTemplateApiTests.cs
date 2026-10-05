using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Micser.Engine.Contracts;
using Micser.Plugins.Main.Modules;

namespace Micser.Engine.Tests;

public class SubgraphTemplateApiTests
{
    [Test]
    public async Task BuiltInTemplates_CantBeChanged()
    {
        await using var factory = new EngineFactory(pluginTemplates: true);
        using var client = factory.CreateAuthorizedClient();
        var builtIn = (await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json))!.First();
        var subgraph = await InstantiateAsync(factory, client, builtIn.Id, new ModulePosition(0, 0));

        using var renamed = await client.PutAsJsonAsync($"/api/subgraph-templates/{builtIn.Id}/name", new RenameSubgraphTemplateRequest("Other"), factory.Json);
        using var removed = await client.DeleteAsync($"/api/subgraph-templates/{builtIn.Id}");
        using var savedOver = await client.PostAsJsonAsync("/api/subgraph-templates", new SaveSubgraphTemplateRequest(subgraph.Id, builtIn.Name, builtIn.Id), factory.Json);
        using var savedAsName = await client.PostAsJsonAsync("/api/subgraph-templates", new SaveSubgraphTemplateRequest(subgraph.Id, builtIn.Name), factory.Json);
        var custom = await SaveAsync(factory, client, subgraph.Id, $"{builtIn.Name} (custom)");
        var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);

        await Assert.That(renamed.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(removed.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(savedOver.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(savedAsName.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(templates!.Single(t => t.Id == builtIn.Id)).IsEquivalentTo(builtIn);
        await Assert.That(custom.IsBuiltIn).IsFalse();
    }

    [Test]
    public async Task BuiltInTemplates_ComeFromThePluginsAndAreNotSaved()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        try
        {
            SubgraphDto subgraph;
            await using (var factory = new EngineFactory(directory, pluginTemplates: true))
            {
                using var client = factory.CreateAuthorizedClient();
                var templates = (await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json))!;
                foreach (var template in templates)
                {
                    var created = await InstantiateAsync(factory, client, template.Id, new ModulePosition(0, 0));
                    var members = await GetMembersAsync(factory, client, created.Id);
                    var connections = (await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections"))!;

                    await Assert.That(members.Length).IsEqualTo(template.Modules.Count);
                    await Assert.That(connections.Count(c => members.Any(m => m.Id == c.TargetModuleId))).IsEqualTo(template.Connections.Count);
                }

                await Assert.That(templates.Select(t => t.Name)).IsEquivalentTo(["Footstep boost", "Night mode", "Voice chat mic"]);
                await Assert.That(templates.All(t => t.IsBuiltIn)).IsTrue();
                subgraph = (await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json))!.First();
            }

            await Assert.That(JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "config.json")))!["templates"]!.AsArray()).IsEmpty();

            await using (var factory = new EngineFactory(directory, pluginTemplates: true))
            {
                using var client = factory.CreateAuthorizedClient();
                var restored = (await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json))!.Single(s => s.Id == subgraph.Id);

                await Assert.That(restored.TemplateId).IsEqualTo(subgraph.TemplateId);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task BuiltInTemplates_RenameTheUsersTemplatesWithTheirName()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        try
        {
            SubgraphTemplateDto footsteps;
            SubgraphTemplateDto night;
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                footsteps = await SaveAsync(factory, client, (await CreateChainAsync(factory, client)).Subgraph.Id, "footstep BOOST");
                night = await SaveAsync(factory, client, (await CreateChainAsync(factory, client)).Subgraph.Id, "Night mode");
                await SaveAsync(factory, client, (await CreateChainAsync(factory, client)).Subgraph.Id, "Night mode (custom)");
            }

            await using (var factory = new EngineFactory(directory, pluginTemplates: true))
            {
                using var client = factory.CreateAuthorizedClient();
                var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);

                await Assert.That(templates!.Single(t => t.Id == footsteps.Id).Name).IsEqualTo("footstep BOOST (custom)");
                await Assert.That(templates!.Single(t => t.Id == night.Id).Name).IsEqualTo("Night mode (custom 2)");
                await Assert.That(templates!.Count(t => t.IsBuiltIn)).IsEqualTo(3);
            }

            // the new names are saved
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);

                await Assert.That(templates!.Select(t => t.Name)).IsEquivalentTo(["footstep BOOST (custom)", "Night mode (custom 2)", "Night mode (custom)"]);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task BuiltInTemplates_WhileTheirPluginIsntLoaded_KeepTheirSubgraphsLinked()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        try
        {
            SubgraphDto subgraph;
            await using (var factory = new EngineFactory(directory, pluginTemplates: true))
            {
                using var client = factory.CreateAuthorizedClient();
                var builtIn = (await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json))!.First();
                subgraph = await InstantiateAsync(factory, client, builtIn.Id, new ModulePosition(0, 0));

                // a change that saves the configuration
                await factory.AddModuleAsync(client, "Gain");
            }

            // the templates are missing, as if their plugin weren't loaded
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var restored = (await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json))!.Single();
                using var update = await client.PostAsync($"/api/subgraphs/{subgraph.Id}/update-from-template", null);

                await Assert.That(restored.TemplateId).IsEqualTo(subgraph.TemplateId);
                await Assert.That(restored.TemplateRevision).IsEqualTo(subgraph.TemplateRevision);
                await Assert.That(update.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

                await factory.AddModuleAsync(client, "Gain");
            }

            await using (var factory = new EngineFactory(directory, pluginTemplates: true))
            {
                using var client = factory.CreateAuthorizedClient();
                using var update = await client.PostAsync($"/api/subgraphs/{subgraph.Id}/update-from-template", null);
                var updated = await update.Content.ReadFromJsonAsync<SubgraphDto>(factory.Json);

                await Assert.That(update.StatusCode).IsEqualTo(HttpStatusCode.OK);
                await Assert.That(updated!.TemplateId).IsEqualTo(subgraph.TemplateId);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task DeleteTemplate_ClearsTheReferences()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (subgraph, _, _) = await CreateChainAsync(factory, client);
        var template = await SaveAsync(factory, client, subgraph.Id, "Chain");

        using var response = await client.DeleteAsync($"/api/subgraph-templates/{template.Id}");
        var subgraphs = await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json);
        var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(subgraphs!.Single().TemplateId).IsNull();
        await Assert.That(subgraphs!.Single().TemplateRevision).IsNull();
        await Assert.That(templates).IsEmpty();
    }

    [Test]
    public async Task InstantiateTemplate_CreatesModulesAndConnections()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (subgraph, first, _) = await CreateChainAsync(factory, client);
        (await client.PutAsJsonAsync<ModuleDto>($"/api/modules/{first.Id}", ((ModuleDto<GainState>)first) with { State = new GainState(-6f) }, factory.Json)).EnsureSuccessStatusCode();
        var template = await SaveAsync(factory, client, subgraph.Id, "Chain");

        var created = await InstantiateAsync(factory, client, template.Id, new ModulePosition(1000, 500));
        var modules = (await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json))!.Where(m => m.SubgraphId == created.Id).ToArray();
        var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

        await Assert.That(created.TemplateId).IsEqualTo(template.Id);
        await Assert.That(created.TemplateRevision).IsEqualTo(1);
        await Assert.That(created.Name).IsEqualTo("Chain");
        await Assert.That(created.Position).IsEqualTo(new ModulePosition(1000, 500));
        await Assert.That(modules.Length).IsEqualTo(2);
        await Assert.That(modules.Select(m => m.TemplateModuleId)).IsEquivalentTo(template.Modules.Select(m => (Guid?)m.Id));
        await Assert.That(modules.OfType<ModuleDto<GainState>>().Select(m => m.State.Gain)).Contains(-6f);
        await Assert.That(connections!.Count(c => modules.Any(m => m.Id == c.SourceModuleId) && modules.Any(m => m.Id == c.TargetModuleId))).IsEqualTo(1);
    }

    [Test]
    public async Task InstantiateTemplate_WithUnavailableModules_IsBadRequestAndTheTemplateIsKept()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        Directory.CreateDirectory(directory);
        var templateId = Guid.NewGuid();
        var config = new JsonObject
        {
            ["version"] = 1,
            ["templates"] = new JsonArray(new JsonObject
            {
                ["id"] = templateId,
                ["name"] = "Missing",
                ["revision"] = 1,
                ["color"] = "Teal",
                ["size"] = new JsonObject { ["width"] = 400, ["height"] = 300 },
                ["modules"] = new JsonArray(new JsonObject { ["type"] = "Nope", ["id"] = Guid.NewGuid(), ["state"] = new JsonObject() }),
                ["connections"] = new JsonArray(),
            }),
        };
        await File.WriteAllTextAsync(Path.Combine(directory, "config.json"), config.ToJsonString());
        try
        {
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);
                using var response = await client.PostAsJsonAsync(
                    $"/api/subgraph-templates/{templateId}/instantiate", new InstantiateSubgraphTemplateRequest(new ModulePosition(0, 0)), factory.Json);

                // a change that saves the configuration
                var gain = await factory.AddModuleAsync(client, "Gain");
                await CreateSubgraphAsync(factory, client, gain.Id);

                await Assert.That(templates!.Single().UnavailableTypes).IsEquivalentTo(["Nope"]);
                await Assert.That(templates!.Single().Modules).IsEmpty();
                await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
            }

            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);

                await Assert.That(templates!.Single().UnavailableTypes).IsEquivalentTo(["Nope"]);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task RenameTemplate_ToAnExistingName_IsConflict()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (first, _, _) = await CreateChainAsync(factory, client);
        var (second, _, _) = await CreateChainAsync(factory, client);
        await SaveAsync(factory, client, first.Id, "First");
        var template = await SaveAsync(factory, client, second.Id, "Second");

        using var conflict = await client.PutAsJsonAsync($"/api/subgraph-templates/{template.Id}/name", new RenameSubgraphTemplateRequest("first"), factory.Json);
        using var renamed = await client.PutAsJsonAsync($"/api/subgraph-templates/{template.Id}/name", new RenameSubgraphTemplateRequest(" Third "), factory.Json);

        await Assert.That(conflict.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That((await renamed.Content.ReadFromJsonAsync<SubgraphTemplateDto>(factory.Json))!.Name).IsEqualTo("Third");
    }

    [Test]
    public async Task SaveTemplate_DuplicateName_IsConflict()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (first, _, _) = await CreateChainAsync(factory, client);
        var (second, _, _) = await CreateChainAsync(factory, client);
        await SaveAsync(factory, client, first.Id, "Chain");

        using var response = await client.PostAsJsonAsync("/api/subgraph-templates", new SaveSubgraphTemplateRequest(second.Id, "chain"), factory.Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task SaveTemplate_OverExisting_KeepsIdAndIncreasesRevision()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (subgraph, _, _) = await CreateChainAsync(factory, client);
        var template = await SaveAsync(factory, client, subgraph.Id, "Chain");

        var saved = await SaveAsync(factory, client, subgraph.Id, "Chain v2", template.Id);
        var updated = await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json);

        await Assert.That(saved.Id).IsEqualTo(template.Id);
        await Assert.That(saved.Revision).IsEqualTo(2);
        await Assert.That(saved.Name).IsEqualTo("Chain v2");
        await Assert.That(saved.Modules.Select(m => m.Id)).IsEquivalentTo(template.Modules.Select(m => m.Id));
        await Assert.That(updated!.Single().TemplateRevision).IsEqualTo(2);
    }

    [Test]
    public async Task SaveTemplate_SetsTheReferenceAndTemplateModuleIds()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (subgraph, first, second) = await CreateChainAsync(factory, client);

        var template = await SaveAsync(factory, client, subgraph.Id, " Chain ");
        var subgraphs = await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json);
        var modules = await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json);

        await Assert.That(template.Name).IsEqualTo("Chain");
        await Assert.That(template.Revision).IsEqualTo(1);
        await Assert.That(template.Color).IsEqualTo(subgraph.Color);
        await Assert.That(template.Modules.Count).IsEqualTo(2);
        await Assert.That(template.Modules.All(m => m.SubgraphId == null && m.TemplateModuleId == null)).IsTrue();
        await Assert.That(template.Connections).HasSingleItem();
        await Assert.That(subgraphs!.Single().TemplateId).IsEqualTo(template.Id);
        await Assert.That(subgraphs!.Single().TemplateRevision).IsEqualTo(1);
        await Assert.That(modules!.Where(m => m.Id == first.Id || m.Id == second.Id).Select(m => m.TemplateModuleId))
            .IsEquivalentTo(template.Modules.Select(m => (Guid?)m.Id));
    }

    [Test]
    public async Task Templates_AreRestoredAfterRestart()
    {
        var directory = EngineFactory.CreateTemporaryDirectory();
        try
        {
            SubgraphTemplateDto template;
            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var (subgraph, _, _) = await CreateChainAsync(factory, client);
                template = await SaveAsync(factory, client, subgraph.Id, "Chain");
            }

            await using (var factory = new EngineFactory(directory))
            {
                using var client = factory.CreateAuthorizedClient();
                var templates = await client.GetFromJsonAsync<SubgraphTemplateDto[]>("/api/subgraph-templates", factory.Json);
                var subgraphs = await client.GetFromJsonAsync<SubgraphDto[]>("/api/subgraphs", factory.Json);

                await Assert.That(templates!.Single().Name).IsEqualTo("Chain");
                await Assert.That(templates!.Single().Modules.Select(m => m.Id)).IsEquivalentTo(template.Modules.Select(m => m.Id));
                await Assert.That(templates!.Single().Connections).IsEquivalentTo(template.Connections);
                await Assert.That(subgraphs!.Single().TemplateId).IsEqualTo(template.Id);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task UpdateSubgraph_CanClearButNotSetTheTemplate()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (subgraph, _, _) = await CreateChainAsync(factory, client);
        var template = await SaveAsync(factory, client, subgraph.Id, "Chain");
        var (other, _, _) = await CreateChainAsync(factory, client);

        using var set = await client.PutAsJsonAsync($"/api/subgraphs/{other.Id}", other with { TemplateId = template.Id, TemplateRevision = 1 }, factory.Json);
        using var cleared = await client.PutAsJsonAsync($"/api/subgraphs/{subgraph.Id}", subgraph with { TemplateId = null, TemplateRevision = 1 }, factory.Json);

        await Assert.That((await set.Content.ReadFromJsonAsync<SubgraphDto>(factory.Json))!.TemplateId).IsNull();
        var detached = (await cleared.Content.ReadFromJsonAsync<SubgraphDto>(factory.Json))!;
        await Assert.That(detached.TemplateId).IsNull();
        await Assert.That(detached.TemplateRevision).IsNull();
    }

    [Test]
    public async Task UpdateSubgraphFromTemplate_KeepsMatchingModulesAndOutsideConnections()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (source, first, second) = await CreateChainAsync(factory, client);
        var template = await SaveAsync(factory, client, source.Id, "Chain");
        var instance = await InstantiateAsync(factory, client, template.Id, new ModulePosition(0, 1000));
        var instanceModules = await GetMembersAsync(factory, client, instance.Id);
        var firstLocalId = template.Modules.Single(m => m.Name == "First").Id;
        var kept = instanceModules.Single(m => m.TemplateModuleId == firstLocalId);
        var removed = instanceModules.Single(m => m.Id != kept.Id);
        var outside = await factory.AddModuleAsync(client, "Gain");
        (await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(outside.Id, "Output", kept.Id, "Input"))).EnsureSuccessStatusCode();

        // the source gets a new gain state, loses its second module, gains a compressor after the first one
        (await client.PutAsJsonAsync<ModuleDto>($"/api/modules/{first.Id}", ((ModuleDto<GainState>)first) with { State = new GainState(-12f) }, factory.Json)).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/modules/{second.Id}")).EnsureSuccessStatusCode();
        var compressor = await AddModuleAsync(factory, client, new CreateModuleRequest("Compressor", SubgraphId: source.Id));
        (await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", compressor.Id, "Input"))).EnsureSuccessStatusCode();
        await SaveAsync(factory, client, source.Id, "Chain", template.Id);

        using var response = await client.PostAsync($"/api/subgraphs/{instance.Id}/update-from-template", null);
        var updated = await response.Content.ReadFromJsonAsync<SubgraphDto>(factory.Json);
        var members = await GetMembersAsync(factory, client, instance.Id);
        var connections = await client.GetFromJsonAsync<ConnectionDto[]>("/api/connections");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(updated!.TemplateRevision).IsEqualTo(2);
        await Assert.That(members.Select(m => m.Id)).Contains(kept.Id);
        await Assert.That(members.Select(m => m.Id)).DoesNotContain(removed.Id);
        await Assert.That(((ModuleDto<GainState>)members.Single(m => m.Id == kept.Id)).State.Gain).IsEqualTo(-12f);
        var added = members.OfType<ModuleDto<CompressorState>>().Single();
        await Assert.That(connections!.Any(c => c.SourceModuleId == outside.Id && c.TargetModuleId == kept.Id)).IsTrue();
        await Assert.That(connections!.Any(c => c.SourceModuleId == kept.Id && c.TargetModuleId == added.Id)).IsTrue();
        await Assert.That(connections!.Any(c => c.SourceModuleId == removed.Id || c.TargetModuleId == removed.Id)).IsFalse();
    }

    [Test]
    public async Task UpdateSubgraphFromTemplate_WithoutTemplate_IsBadRequest()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var (subgraph, _, _) = await CreateChainAsync(factory, client);

        using var response = await client.PostAsync($"/api/subgraphs/{subgraph.Id}/update-from-template", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    private static async Task<ModuleDto> AddModuleAsync(EngineFactory factory, HttpClient client, CreateModuleRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/modules", request, factory.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ModuleDto>(factory.Json))!;
    }

    /// <summary>
    /// A subgraph with two gains named "First" and "Second", the first connected to the second.
    /// </summary>
    private static async Task<(SubgraphDto Subgraph, ModuleDto First, ModuleDto Second)> CreateChainAsync(EngineFactory factory, HttpClient client)
    {
        var first = await AddModuleAsync(factory, client, new CreateModuleRequest("Gain", "First", new ModulePosition(0, 0)));
        var second = await AddModuleAsync(factory, client, new CreateModuleRequest("Gain", "Second", new ModulePosition(300, 0)));
        (await client.PostAsJsonAsync("/api/connections", new CreateConnectionRequest(first.Id, "Output", second.Id, "Input"))).EnsureSuccessStatusCode();
        var subgraph = await CreateSubgraphAsync(factory, client, first.Id, second.Id);
        var modules = await GetMembersAsync(factory, client, subgraph.Id);
        return (subgraph, modules.Single(m => m.Id == first.Id), modules.Single(m => m.Id == second.Id));
    }

    private static async Task<SubgraphDto> CreateSubgraphAsync(EngineFactory factory, HttpClient client, params Guid[] moduleIds)
    {
        var response = await client.PostAsJsonAsync(
            "/api/subgraphs", new CreateSubgraphRequest(null, new ModulePosition(-40, -80), new SubgraphSize(700, 300), moduleIds), factory.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubgraphDto>(factory.Json))!;
    }

    private static async Task<ModuleDto[]> GetMembersAsync(EngineFactory factory, HttpClient client, Guid subgraphId)
    {
        return [.. (await client.GetFromJsonAsync<ModuleDto[]>("/api/modules", factory.Json))!.Where(m => m.SubgraphId == subgraphId)];
    }

    private static async Task<SubgraphDto> InstantiateAsync(EngineFactory factory, HttpClient client, Guid templateId, ModulePosition position)
    {
        var response = await client.PostAsJsonAsync($"/api/subgraph-templates/{templateId}/instantiate", new InstantiateSubgraphTemplateRequest(position), factory.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubgraphDto>(factory.Json))!;
    }

    private static async Task<SubgraphTemplateDto> SaveAsync(EngineFactory factory, HttpClient client, Guid subgraphId, string name, Guid? templateId = null)
    {
        var response = await client.PostAsJsonAsync("/api/subgraph-templates", new SaveSubgraphTemplateRequest(subgraphId, name, templateId), factory.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubgraphTemplateDto>(factory.Json))!;
    }
}
