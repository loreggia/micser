using System;
using System.Resources;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Micser.App.Infrastructure.Localization
{
    /// <summary>
    /// Markup extension that provides a binding to a localized resource value.
    /// </summary>
    public class ResxExtension : MarkupExtension
    {
        /// <inheritdoc />
        public ResxExtension()
        {
        }

        /// <inheritdoc />
        public ResxExtension(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Gets or sets the resource key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the resource manager containing the resource. If this is null, the inherited value of the LocalizationManager.ResourceManager attached property is used.
        /// </summary>
        public ResourceManager ResourceManager { get; set; }

        /// <summary>
        /// Provides a binding to a <see cref="ResourceData"/> instance.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var resourceManager = ResourceManager;

            if (resourceManager == null)
            {
                var provideValueTarget = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

                if (provideValueTarget.TargetObject is DependencyObject obj)
                {
                    resourceManager = LocalizationManager.GetResourceManager(obj);
                }
            }

            var binding = new Binding(nameof(ResourceData.Value))
            {
                Source = new ResourceData(resourceManager, Key)
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}