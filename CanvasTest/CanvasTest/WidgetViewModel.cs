using System.Diagnostics;
using System.Windows;

namespace CanvasTest
{
    public class WidgetViewModel : Bindable
    {
        private Point _position;

        public Point Position
        {
            get => _position;
            set
            {
                SetProperty(ref _position, value);
                Debug.WriteLine($"VM: {value}");
            }
        }
    }
}