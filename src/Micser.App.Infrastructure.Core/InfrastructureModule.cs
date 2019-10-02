using Micser.App.Infrastructure.Localization;
using Micser.App.Infrastructure.Resources;
using Micser.App.Infrastructure.Themes;
using Micser.Common;
using Micser.Common.Extensions;
using System;
using System.Windows;

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
            var resourceRegistry = containerProvider.Resolve<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("/Micser.App.Infrastructure;component/Themes/Generic.xaml", UriKind.Relative) });
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