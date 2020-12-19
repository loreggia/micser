using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.Common
{
    /// <summary>
    /// Base interface for plugin modules.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets this plugins UI module name (configured in plugin webpack config).
        /// </summary>
        string? UIModuleName { get; }

        /// <summary>
        /// Lets the module register its types in the DI container.
        /// </summary>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Initializes the module (after all modules have registered their types in the DI container).
        /// </summary>
        void Initialize(IServiceProvider serviceProvider);
    }
}