using Microsoft.AspNetCore.Builder;
using Micser.Common;

namespace Micser.Engine.Infrastructure
{
    /// <summary>
    /// Base interface for engine plugins.
    /// </summary>
    public interface IEngineModule : IModule
    {
        /// <summary>
        /// Allows configuring of the web app.
        /// </summary>
        void Configure(IApplicationBuilder app);
    }
}