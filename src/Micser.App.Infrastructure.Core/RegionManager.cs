using System.Windows;

namespace Micser.App.Infrastructure
{
    public class RegionManager : IRegionManager
    {
        public static readonly DependencyProperty RegionNameProperty = DependencyProperty.RegisterAttached(
            "RegionName", typeof(string), typeof(RegionManager), new PropertyMetadata(null, OnRegionNamePropertyChanged));

        public static string GetRegionName(DependencyObject element)
        {
            return (string)element.GetValue(RegionNameProperty);
        }

        public static void SetRegionName(DependencyObject element, string value)
        {
            element.SetValue(RegionNameProperty, value);
        }

        public void RequestNavigate(string regionName, string uri)
        {
            throw new System.NotImplementedException();
        }

        private static void OnRegionNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}