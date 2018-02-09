using System.Collections.Generic;
using System.Collections.ObjectModel;
using Micser.Infrastructure;
using Micser.Infrastructure.ViewModels;
using Micser.Main.ViewModels.Widgets;
using Micser.Main.Views.Widgets;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private readonly IList<WidgetViewModel> _widgets;

        private IEnumerable<WidgetDescription> _availableWidgets;

        public MainViewModel(IWidgetFactory widgetFactory)
        {
            WidgetFactory = widgetFactory;
            _widgets = new ObservableCollection<WidgetViewModel>();
        }

        public IEnumerable<WidgetDescription> AvailableWidgets
        {
            get => _availableWidgets;
            set => SetProperty(ref _availableWidgets, value);
        }

        public IWidgetFactory WidgetFactory { get; }
        public IEnumerable<WidgetViewModel> Widgets => _widgets;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            _widgets.Add(new DeviceInputViewModel());
            _widgets.Add(new DeviceOutputViewModel());
        }
    }
}