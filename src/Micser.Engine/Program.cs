using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace Micser.Engine
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        internal static void Main(params string[] arguments)
        {
            var service = new MicserService();

            if (arguments.Any(a => string.Equals("manual", a, StringComparison.InvariantCultureIgnoreCase)))
            {
                service.ManualStart();

                while (true)
                {
                    Thread.Sleep(100);
                }
            }

            ServiceBase.Run(new ServiceBase[] { service });
        }
    }
}