using Micser.App.Infrastructure.Localization;
using Micser.App.Infrastructure.Resources;
using Micser.Common;
using System;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// UI module performing default initialization tasks.
    /// </summary>
    public class InfrastructureModule : IAppModule
    {
        /// <inheritdoc />
        public InfrastructureModule()
        {
            LocalizationManager.UiCultureChanged += OnUiCultureChanged;
        }

        /// <inheritdoc />
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        /// <inheritdoc />
        public void RegisterTypes(IContainerProvider containerRegistry)
        {
        }

        private static void OnUiCultureChanged(object sender, EventArgs e)
        {
            Strings.Culture = LocalizationManager.UiCulture;
        }
    }
}