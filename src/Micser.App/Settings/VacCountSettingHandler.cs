using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;
using Micser.Common;
using Micser.Common.Api;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using NLog;

namespace Micser.App.Settings
{
    public class VacCountSettingHandler : ISettingHandler
    {
        private readonly EngineApiClient _engineApiClient;
        private readonly ILogger _logger;

        public VacCountSettingHandler(EngineApiClient engineApiClient, ILogger logger)
        {
            _logger = logger;
            _engineApiClient = engineApiClient;
        }

        public async Task<object> LoadSettingAsync(object value)
        {
            return await Task.Run(GetRegistryValue).ConfigureAwait(false);
        }

        public async Task<object> SaveSettingAsync(object value)
        {
            var iValue = Convert.ToInt32(value);
            MathExtensions.Clamp(ref iValue, 1, Globals.MaxVacCount);

            var a = Globals.DriverUtility.ArgumentNameChars[0];

            try
            {
                await _engineApiClient.StopAsync().ConfigureAwait(false);

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
                }
                else if (!process.WaitForExit(30000))
                {
                    _logger.Error("The driver utility did not finish within 30 seconds. Aborting...");
                    process.Kill();
                }

                if (process.ExitCode != Globals.DriverUtility.ReturnCodes.Success)
                {
                    _logger.Error($"Driver utility returned with exit code {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                await _engineApiClient.StartAsync().ConfigureAwait(false);
            }

            return GetRegistryValue();
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