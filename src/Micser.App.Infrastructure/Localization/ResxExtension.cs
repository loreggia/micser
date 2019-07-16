using System;
using System.Resources;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Micser.App.Infrastructure.Localization
{
    public class ResxExtension : MarkupExtension
    {
        public ResxExtension()
        {
        }

        public ResxExtension(string key)
        {
            Key = key;
        }

        public string Key { get; set; }

        public ResourceManager ResourceManager { get; set; }

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