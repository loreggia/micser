using Micser.Common.Widgets;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Widgets
{
    public abstract class WidgetViewModel : ViewModel
    {
        private readonly ObservableCollection<ConnectorViewModel> _inputConnectors;

        private readonly ObservableCollection<ConnectorViewModel> _outputConnectors;

        private ICommand _deleteCommand;
        private string _header;

        private long _id;

        private WidgetState _loadingWidgetState;

        private string _name;

        private Point _position;
        private Size _size;

        protected WidgetViewModel()
        {
            _inputConnectors = new ObservableCollection<ConnectorViewModel>();
            _outputConnectors = new ObservableCollection<ConnectorViewModel>();

            DeleteCommand = new DelegateCommand(OnDeleteWidget);
        }

        public ICommand DeleteCommand
        {
            get => _deleteCommand;
            set => SetProperty(ref _deleteCommand, value);
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public IEnumerable<ConnectorViewModel> InputConnectors => _inputConnectors;

        public bool IsInitialized { get; protected set; }

        public abstract Type ModuleType { get; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public IEnumerable<ConnectorViewModel> OutputConnectors => _outputConnectors;

        public Point Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public Size Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public virtual WidgetState GetState()
        {
            return new WidgetState
            {
                Name = Name,
                Position = Position,
                Size = Size
            };
        }

        public virtual void Initialize()
        {
            IsInitialized = true;

            if (_loadingWidgetState != null)
            {
                LoadState(_loadingWidgetState);
                _loadingWidgetState = null;
            }
        }

        public virtual void LoadState(WidgetState state)
        {
            if (!IsInitialized)
            {
                _loadingWidgetState = state;
                return;
            }

            Name = state.Name;
            Position = state.Position;
            Size = state.Size;
        }

        protected ConnectorViewModel AddInput(string name)
        {
            var input = new ConnectorViewModel(name, this, null);
            input.ConnectionChanged += OnInputConnectionChanged;
            AddInput(input);
            return input;
        }

        protected virtual void AddInput(ConnectorViewModel input)
        {
            _inputConnectors.Add(input);
        }

        protected ConnectorViewModel AddOutput(string name)
        {
            var output = new ConnectorViewModel(name, this, null);
            output.ConnectionChanged += OnOutputConnectionChanged;
            AddOutput(output);
            return output;
        }

        protected virtual void AddOutput(ConnectorViewModel output)
        {
            _outputConnectors.Add(output);
        }

        protected virtual void OnDeleteWidget()
        {
        }

        protected virtual void OnInputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
        }

        protected virtual void OnOutputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
        }
    }
}