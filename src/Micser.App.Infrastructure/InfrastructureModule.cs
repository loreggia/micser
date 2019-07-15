using Prism.Ioc;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// UI module performing default initialization tasks.
    /// </summary>
    public class InfrastructureModule : IAppModule
    {
        /// <inheritdoc />
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        /// <inheritdoc />
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}