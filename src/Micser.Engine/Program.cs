using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Micser.Engine
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static async Task Main(params string[] arguments)
        {
            TrySetPriority();

            var isService = !(Debugger.IsAttached || arguments.Contains("--console"));

            var builder = Host.CreateDefaultBuilder(arguments)
                .ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup<Startup>());

            if (isService)
            {
                builder.UseWindowsService();
            }

            await builder.Build().StartAsync();
        }

        private static void TrySetPriority()
        {
            try
            {
                // try setting process priority to high
                using (var process = Process.GetCurrentProcess())
                {
                    process.PriorityClass = ProcessPriorityClass.High;
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}