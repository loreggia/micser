using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Micser.Engine.Tests;

public class ShutdownTests
{
    [Test]
    public async Task Shutdown_StopsTheApplication()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateAuthorizedClient();
        var stopping = factory.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        var stopped = new TaskCompletionSource();
        using var registration = stopping.Register(() => stopped.TrySetResult());

        using var response = await client.PostAsync("/api/engine/shutdown", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        // the endpoint stops the application once the response has completed
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(stopping.IsCancellationRequested).IsTrue();
    }

    [Test]
    public async Task Shutdown_WithoutToken_IsUnauthorized()
    {
        await using var factory = new EngineFactory();
        using var client = factory.CreateClient();
        var stopping = factory.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        using var response = await client.PostAsync("/api/engine/shutdown", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(stopping.IsCancellationRequested).IsFalse();
    }
}
