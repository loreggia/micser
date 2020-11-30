using Blazorise;
using Blazorise.Icons.Material;
using Blazorise.Material;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
            builder
                .ConfigureServices(services =>
                {
                    services.AddRazorPages();
                    services.AddServerSideBlazor();

                    services.AddBlazorise(options =>
                        {
                            options.ChangeTextOnKeyPress = true;
                        })
                        .AddMaterialProviders()
                        .AddMaterialIcons();

                    services.AddScoped<WeatherForecastService>();
                })
                .Configure((context, app) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
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
                    var assembly = typeof(UiHostingStartup).Assembly;
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        RequestPath = "/_ui",
                        FileProvider = new EmbeddedFileProvider(assembly, $"{assembly.GetName().Name}.wwwroot")
                    });

                    app.UseRouting();

                    app.ApplicationServices
                        .UseMaterialProviders()
                        .UseMaterialIcons();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapBlazorHub();
                        endpoints.MapFallbackToPage("/_Host");
                    });
                });
        }
    }
}