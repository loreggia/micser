using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace CanvasTest
{
    public class MainViewModel : Bindable
    {
        private readonly ICollection<WidgetViewModel> _widgets;

        public MainViewModel()
        {
            _widgets = new ObservableCollection<WidgetViewModel>();

            Initialize();
        }

        public IEnumerable<WidgetViewModel> Widgets => _widgets;

        public void Initialize()
        {
            _widgets.Add(new WidgetViewModel { Position = new Point(20, 50) });
        }
    }
}