using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace Micser.Engine
{
    internal static class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.ConfigureKestrel(options =>
                        options.ListenLocalhost(5001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2));
                    webHostBuilder.UseStartup<Startup>();
                });
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            TrySetPriority();

            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var hostBuilder = CreateHostBuilder(args);

            if (isService)
            {
                hostBuilder.UseWindowsService();
            }

            hostBuilder.Build().Run();
        }

        private static void TrySetPriority()
        {
            try
            {
                // try setting process priority to high
                using var process = Process.GetCurrentProcess();
                process.PriorityClass = ProcessPriorityClass.High;
            }
            catch
            {
                // ignore
            }
        }
    }
}