using System.Net;

namespace Micser.Engine.Tests;

public class AccessTests
{
    [Test]
    public async Task Api_WithoutToken_IsUnauthorized()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/modules");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Api_WithToken_IsAllowed()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();

        using var response = await client.GetAsync("/api/modules");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Api_WithWrongToken_IsUnauthorized()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            "wrong"
        );

        using var response = await client.GetAsync("/api/modules");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Health_WithoutToken_ReturnsOk()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Hub_WithoutToken_IsUnauthorized()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/hubs/engine/negotiate?negotiateVersion=1", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
