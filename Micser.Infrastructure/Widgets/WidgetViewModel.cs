using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace Micser.Infrastructure.Widgets
{
    public abstract class WidgetViewModel : ViewModel
    {
        private readonly ObservableCollection<ConnectorViewModel> _inputConnectors;
        private readonly ObservableCollection<ConnectorViewModel> _outputConnectors;
        private string _header;
        private string _name;
        private Point _position;

        protected WidgetViewModel()
        {
            _inputConnectors = new ObservableCollection<ConnectorViewModel>();
            _outputConnectors = new ObservableCollection<ConnectorViewModel>();
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public IEnumerable<ConnectorViewModel> InputConnectors => _inputConnectors;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public IEnumerable<ConnectorViewModel> OutputConnectors => _outputConnectors;

        public Point Position
        {
            get => _position;
            set
            {
                SetProperty(ref _position, value);
                Debug.WriteLine($"WidgetViewModel.Position.set(): {value}");
            }
        }

        public virtual void Initialize()
        {
        }

        protected virtual void AddInput(ConnectorViewModel input)
        {
            _inputConnectors.Add(input);
        }

        protected virtual void AddOutput(ConnectorViewModel output)
        {
            _outputConnectors.Add(output);
        }
    }
}