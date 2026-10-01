using System.Net;

namespace Micser.Shell.Tests;

public class EngineLocatorTests
{
    [Test]
    public async Task Find_RunningEngine_ReturnsIt()
    {
        using var file = new TemporaryFile("""{ "url": "http://127.0.0.1:5000", "token": "secret", "processId": 42 }""");
        using var http = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        var locator = new EngineLocator(file.Path, http, processId => processId == 42);

        var engine = await locator.FindAsync();

        await Assert.That(engine).IsEqualTo(new EngineInfo(new Uri("http://127.0.0.1:5000"), "secret", 42));
    }

    [Test]
    public async Task Find_EngineWithoutToken_ReturnsNullToken()
    {
        using var file = new TemporaryFile("""{ "url": "http://127.0.0.1:5080", "token": null, "processId": 42 }""");
        using var http = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        var locator = new EngineLocator(file.Path, http, _ => true);

        var engine = await locator.FindAsync();

        await Assert.That(engine!.Token).IsNull();
    }

    [Test]
    public async Task Find_ProcessIsNotAnEngine_ReturnsNull()
    {
        // e.g. the discovery file of a crashed engine whose process ID was reused
        using var file = new TemporaryFile("""{ "url": "http://127.0.0.1:5000", "token": "secret", "processId": 42 }""");
        using var http = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        var locator = new EngineLocator(file.Path, http, _ => false);

        await Assert.That(await locator.FindAsync()).IsNull();
    }

    [Test]
    public async Task Find_HealthCheckFails_ReturnsNull()
    {
        using var file = new TemporaryFile("""{ "url": "http://127.0.0.1:5000", "token": "secret", "processId": 42 }""");
        using var failing = new HttpClient(new FakeHandler(HttpStatusCode.ServiceUnavailable));
        using var unreachable = new HttpClient(new FakeHandler(null));

        await Assert.That(await new EngineLocator(file.Path, failing, _ => true).FindAsync()).IsNull();
        await Assert.That(await new EngineLocator(file.Path, unreachable, _ => true).FindAsync()).IsNull();
    }

    [Test]
    [Arguments("")]
    [Arguments("{ not json")]
    [Arguments("""{ "token": "secret", "processId": 42 }""")]
    public async Task Find_InvalidDiscoveryFile_ReturnsNull(string content)
    {
        using var file = new TemporaryFile(content);
        using var http = new HttpClient(new FakeHandler(HttpStatusCode.OK));

        await Assert.That(await new EngineLocator(file.Path, http, _ => true).FindAsync()).IsNull();
    }

    [Test]
    public async Task Find_NoDiscoveryFile_ReturnsNull()
    {
        using var http = new HttpClient(new FakeHandler(HttpStatusCode.OK));
        var locator = new EngineLocator(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "engine.json"), http, _ => true);

        await Assert.That(await locator.FindAsync()).IsNull();
    }

    [Test]
    public async Task Find_ChecksHealthEndpointOfTheEngine()
    {
        using var file = new TemporaryFile("""{ "url": "http://127.0.0.1:5000", "token": "secret", "processId": 42 }""");
        var handler = new FakeHandler(HttpStatusCode.OK);
        using var http = new HttpClient(handler);

        await new EngineLocator(file.Path, http, _ => true).FindAsync();

        await Assert.That(handler.LastRequest).IsEqualTo(new Uri("http://127.0.0.1:5000/api/health"));
    }

    /// <summary>
    /// Answers every request with a status code, or fails like an unreachable server when the code is null.
    /// </summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;

        public FakeHandler(HttpStatusCode? statusCode)
        {
            _statusCode = statusCode;
        }

        public Uri? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request.RequestUri;
            return _statusCode is { } statusCode
                ? Task.FromResult(new HttpResponseMessage(statusCode))
                : Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused."));
        }
    }

    private sealed class TemporaryFile : IDisposable
    {
        public TemporaryFile(string content)
        {
            Path = System.IO.Path.GetTempFileName();
            File.WriteAllText(Path, content);
        }

        public string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
