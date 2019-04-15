using NLog;
using System.Collections.Generic;

namespace Micser.DriverUtility
{
    public class DriverInstaller
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public DriverInstaller()
        {
        }

        public IEnumerable<string> GetInstalledDevices()
        {
            return null;
        }
    }
}