using Micser.Infrastructure;

namespace Micser.Main.ViewModels.Widgets
{
    public abstract class WidgetViewModel : ViewModel
    {
        private string _header;
        private string _name;

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}