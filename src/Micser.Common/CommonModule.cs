using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.Common.UI;

namespace Micser.Common
{
    public class CommonModule : IModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<JsDraggable>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
        }
    }
}