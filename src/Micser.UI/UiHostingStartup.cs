using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Micser.UI;
using Micser.UI.Infrastructure;

[assembly: HostingStartup(typeof(UiHostingStartup))]

namespace Micser.UI
{
    public class UiHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter, UiHostingStartupFilter>();

                services.AddControllers();
                services.AddSignalR(options =>
                {
                    // increase max message size for spectrum data
                    options.MaximumReceiveMessageSize = 1024 * 1000;
                });

                services.AddSingleton<IFileProvider>(sp => new PhysicalFileProvider(sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath, ExclusionFilters.Sensitive));
                // In production, the React files will be served from this directory
                services.AddSpaStaticFiles(configuration => configuration.RootPath = "App/build");
            });
        }

        public class UiHostingStartupFilter : IStartupFilter
        {
            private readonly IWebHostEnvironment _environment;
            private readonly ILogger<UiHostingStartup> _logger;

            public UiHostingStartupFilter(IWebHostEnvironment environment, ILogger<UiHostingStartup> logger)
            {
                _environment = environment;
                _logger = logger;
            }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    if (_environment.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }
                    else
                    {
                        app.UseHsts();
                    }

                    app.UseHttpsRedirection();

                    app.UseApiExceptionHandler(_logger);

                    app.UseRouting();

                    app.UseStaticFiles();
                    app.UseSpecificSpaStaticFiles(options =>
                        options
                            .WithRootPath(_environment.IsDevelopment()
                                ? "App/public"
                                : "App/build")
                            .WithRequestPaths("/locales")
                    );
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });

                    app.UseSpa(spa =>
                    {
                        if (_environment.IsDevelopment())
                        {
                            spa.Options.SourcePath = "..\\Micser.UI\\App";
                            spa.UseReactDevelopmentServer("start");
                        }
                        else
                        {
                            spa.Options.SourcePath = "App";
                        }
                    });

                    next(app);
                };
            }
        }
    }
}