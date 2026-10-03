using Microsoft.Extensions.FileProviders;

namespace Micser.Engine.Plugins;

public static class PluginFiles
{
    /// <summary>
    /// Serves the widget bundle folder of each loaded plugin at <c>/plugins/{id}/...</c> (see <see cref="PluginService.GetWebUrl"/>). The
    /// files need no token, since the browser imports them as modules.
    /// </summary>
    public static IApplicationBuilder UsePluginFiles(this IApplicationBuilder app, PluginCatalog catalog)
    {
        foreach (var plugin in catalog.Plugins.Where(p => p.IsLoaded && p.Manifest!.WebPath != null))
        {
            var webFolder = Path.GetDirectoryName(plugin.Manifest!.WebPath)?.Replace('\\', '/') ?? "";
            var directory = Path.Combine(plugin.Directory, webFolder);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(directory),
                RequestPath = $"/plugins/{plugin.Id}" + (webFolder.Length > 0 ? "/" + webFolder : ""),

                // plugins change with engine restarts, which keep the URL
                OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache",
            });
        }

        return app;
    }
}
