using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Micser.UI;
using Micser.UI.Data;

[assembly: HostingStartup(typeof(UiHostingStartup))]

namespace Micser.UI
{
    public class UiHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<WeatherForecastService>();

                services.AddSingleton<IStartupFilter, UiHostingStartupFilter>();
            });
        }

        public class UiHostingStartupFilter : IStartupFilter
        {
            private readonly IWebHostEnvironment _environment;

            public UiHostingStartupFilter(IWebHostEnvironment environment)
            {
                _environment = environment;
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
                        app.UseExceptionHandler("/Error");
                        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                        app.UseHsts();
                    }

                    app.UseHttpsRedirection();

                    app.UseStaticFiles();

                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapBlazorHub();
                        endpoints.MapFallbackToPage("/_Host");
                    });

                    next(app);
                };
            }
        }
    }
}