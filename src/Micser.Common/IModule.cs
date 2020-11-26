using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.Common
{
    /// <summary>
    /// Base interface for plugin modules.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Lets the module register its types in the DI container.
        /// </summary>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Initializes the module (after all modules have registered their types in the DI container).
        /// </summary>
        void Initialize(IApplicationBuilder app);
    }
}