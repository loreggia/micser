using Microsoft.Win32;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Settings;
using Micser.Common;
using Micser.Common.Extensions;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Micser.App.Settings
{
    public class VacCountSettingHandler : ISettingHandler
    {
        protected readonly ILogger _logger;
        private readonly EngineApiClient _engineApiClient;

        public VacCountSettingHandler(EngineApiClient engineApiClient, ILogger logger)
        {
            _logger = logger;
            _engineApiClient = engineApiClient;
        }

        public async Task<object> LoadSettingAsync(object value)
        {
            return GetRegistryValue();
        }

        public async Task<object> SaveSettingAsync(object value)
        {
            var iValue = Convert.ToInt32(value);
            MathExtensions.Clamp(ref iValue, 1, Globals.MaxVacCount);

            var currentValue = GetRegistryValue();

            if (iValue == currentValue)
            {
                return iValue;
            }

            var a = Globals.DriverUtility.ArgumentNameChars[0];

            try
            {
                await _engineApiClient.StopAsync();

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Globals.DriverUtility.ProgramName,
                        Arguments = $"{a}{Globals.DriverUtility.Arguments.DeviceCount} {iValue}",
                        WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                if (!process.Start())
                {
                    _logger.Error("Could not start the driver utility.");
                    return currentValue;
                }

                if (!process.WaitForExit(30000))
                {
                    _logger.Error("The driver utility did not finish within 30 seconds. Aborting...");
                    process.Kill();
                    return currentValue;
                }

                var exitCode = process.ExitCode;

                if (exitCode != Globals.DriverUtility.ReturnCodes.Success)
                {
                    _logger.Error($"Driver utility returned with exit code {exitCode}");
                    return currentValue;
                }

                return iValue;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return currentValue;
            }
            finally
            {
                await _engineApiClient.StartAsync();
            }
        }

        private int GetRegistryValue()
        {
            try
            {
                var registryKey = Registry.CurrentUser.CreateSubKey(Globals.UserRegistryRoot, false);
                return (int)registryKey.GetValue(Globals.RegistryValues.VacCount, 1);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return 1;
            }
        }
    }
}