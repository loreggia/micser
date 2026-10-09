var builder = DistributedApplication.CreateBuilder(args);

// not proxied, so the engine can bind the endpoint's URL itself: its "Urls" setting would override ASPNETCORE_URLS
var engine = builder
    .AddProject<Projects.Micser_Engine>("engine")
    .WithHttpEndpoint(isProxied: false)
    .WithHttpHealthCheck("/api/health");
engine.WithEnvironment("Urls", engine.GetEndpoint("http"));

// npm workspaces share the root node_modules, so dependencies come from "npm install" at the root
#pragma warning disable ASPIREBROWSERLOGS001 // preview: browser console logs in the dashboard
var web = builder
    .AddViteApp("web", "../../src/Web")
    .WithEnvironment("MICSER_ENGINE_URL", engine.GetEndpoint("http"))
    .WaitFor(engine)
    .WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

// the shell finds the engine through its discovery file and shows the Vite UI
builder
    .AddProject<Projects.Micser_Shell>("shell")
    .WithArgs(context =>
    {
        context.Args.Add("--ui");
        context.Args.Add(web.GetEndpoint("http"));
    })
    .WaitFor(web)
    .WithExplicitStart();

builder.Build().Run();
