using System;
using System.Windows;

namespace Micser.Main.Themes
{
    public class ResourceManager
    {
        private static ResourceDictionary _sharedDictionary;

        public static ResourceDictionary SharedDictionary
        {
            get
            {
                if (_sharedDictionary == null)
                {
                    _sharedDictionary = new ResourceDictionary();
                    var generic = (ResourceDictionary)Application.LoadComponent(new Uri("/Micser.Main;component/Themes/Generic.xaml", UriKind.Relative));
                    var widget = (ResourceDictionary)Application.LoadComponent(new Uri("/Micser.Main;component/Themes/Widget.xaml", UriKind.Relative));
                    var widgetPanel = (ResourceDictionary)Application.LoadComponent(new Uri("/Micser.Main;component/Themes/WidgetPanel.xaml", UriKind.Relative));
                    _sharedDictionary.MergedDictionaries.Add(generic);
                    _sharedDictionary.MergedDictionaries.Add(widget);
                    _sharedDictionary.MergedDictionaries.Add(widgetPanel);
                }

                return _sharedDictionary;
            }
        }
    }
}