using Micser.Common.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Micser.App.Infrastructure.Widgets
{
    public abstract class WidgetViewModel : ViewModel
    {
        private readonly ObservableCollection<ConnectorViewModel> _inputConnectors;
        private readonly ObservableCollection<ConnectorViewModel> _outputConnectors;

        private string _header;
        private long _id;
        private bool _isMuted;
        private bool _isSelected;
        private ModuleState _loadingWidgetState;
        private string _name;
        private Point _position;
        private Size _size;
        private bool _useSystemVolume;
        private float _volume;

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

        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public IEnumerable<ConnectorViewModel> InputConnectors => _inputConnectors;

        public bool IsInitialized { get; protected set; }

        public bool IsMuted
        {
            get => _isMuted;
            set => SetProperty(ref _isMuted, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

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

        public bool UseSystemVolume
        {
            get => _useSystemVolume;
            set => SetProperty(ref _useSystemVolume, value);
        }

        public float Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        public virtual ModuleState GetState()
        {
            return new ModuleState
            {
                Data =
                {
                    {AppGlobals.ModuleStateKeys.Name, Name},
                    {AppGlobals.ModuleStateKeys.Left, Position.X},
                    {AppGlobals.ModuleStateKeys.Top, Position.Y},
                    {AppGlobals.ModuleStateKeys.Width, Size.Width},
                    {AppGlobals.ModuleStateKeys.Height, Size.Height}
                }
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

        public virtual void LoadState(ModuleState state)
        {
            if (!IsInitialized)
            {
                _loadingWidgetState = state;
                return;
            }

            Name = state.Data.GetObject<string>(AppGlobals.ModuleStateKeys.Name);

            var left = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Left);
            var top = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Top);
            Position = new Point(left, top);

            var width = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Width);
            var height = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Height);
            Size = new Size(width, height);
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

        protected virtual void OnInputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
        }

        protected virtual void OnOutputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
        }
    }
}