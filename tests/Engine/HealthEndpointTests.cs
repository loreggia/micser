using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Micser.Engine.Tests;

public class HealthEndpointTests
{
    [Test]
    public async Task Health_ReturnsOk()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
