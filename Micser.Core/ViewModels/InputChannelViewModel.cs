using Micser.Core.Common;

namespace Micser.Core.ViewModels
{
    public class InputChannelViewModel : Bindable
    {
        private string _name;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}