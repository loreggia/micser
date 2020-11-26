using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Micser.Common.Extensions;
using Micser.Engine.Infrastructure;

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
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddNlog("Micser.Engine.log");
                    services.AddPlugins<IEngineModule>();
                    services.AddHostedService<MicserService>();
                });

            if (isService)
            {
                await builder.RunAsServiceAsync();
            }
            else
            {
                await builder.RunConsoleAsync();
            }
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