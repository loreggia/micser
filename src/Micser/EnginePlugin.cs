using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common;
using Micser.Common.Extensions;
using Micser.Common.Settings;

namespace Micser
{
    public class EnginePlugin : IPlugin
    {
        public string? UIModuleName => null;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSetting(new SettingDefinition(Globals.SettingKeys.UpdateCheck)
            {
                DefaultValue = true
            });
            services.AddSetting(new SettingDefinition(Globals.SettingKeys.IsEngineRunning)
            {
                DefaultValue = true
            });
            services.AddSetting(new SettingDefinition(Globals.SettingKeys.ResumeDelay)
            {
                DefaultValue = 10
            });
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}