using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Micser.App.Infrastructure.Localization;
using Micser.App.Infrastructure.Resources;
using Micser.App.Infrastructure.Themes;

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
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        /// <inheritdoc />
        public void Initialize(IServiceProvider serviceProvider)
        {
            var resourceRegistry = serviceProvider.GetRequiredService<IResourceRegistry>();
            resourceRegistry.Add(new ResourceDictionary { Source = new Uri("/Micser.App.Infrastructure;component/Themes/Generic.xaml", UriKind.Relative) });
        }

        private static void OnUiCultureChanged(object sender, EventArgs e)
        {
            Strings.Culture = LocalizationManager.UiCulture;
        }
    }
}