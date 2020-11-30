using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Micser.UI
{
    internal static class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "Micser.UI");
                });
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);
            hostBuilder.Build().Run();
        }
    }
}