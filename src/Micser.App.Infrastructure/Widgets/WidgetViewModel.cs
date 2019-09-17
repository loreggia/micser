using Micser.Common.Extensions;
using Micser.Common.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// The base view model for all widgets.
    /// </summary>
    public abstract class WidgetViewModel : ViewModel
    {
        private readonly ObservableCollection<ConnectorViewModel> _inputConnectors;
        private readonly ObservableCollection<ConnectorViewModel> _outputConnectors;
        private long _id;
        private bool _isEnabled;
        private bool _isSelected;
        private ModuleState _loadingWidgetState;
        private string _name;
        private Point _position;
        private Size _size;

        /// <inheritdoc />
        protected WidgetViewModel()
        {
            _inputConnectors = new ObservableCollection<ConnectorViewModel>();
            _outputConnectors = new ObservableCollection<ConnectorViewModel>();
        }

        /// <summary>
        /// Gets or sets the widget's ID. This corresponds to the module ID in the engine.
        /// </summary>
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets the available input connectors.
        /// </summary>
        public IEnumerable<ConnectorViewModel> InputConnectors => _inputConnectors;

        /// <summary>
        /// Gets or sets a value that indicates whether the module is enabled.
        /// </summary>
        [SaveState(true)]
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// Gets a value whether the widget is initialized, meaning <see cref="Initialize"/> has been called.
        /// </summary>
        public bool IsInitialized { get; protected set; }

        /// <summary>
        /// Gets or sets a value whether this widget is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Gets or sets this widget's corresponding module type in the engine.
        /// </summary>
        public abstract Type ModuleType { get; }

        /// <summary>
        /// Gets or sets the widget's name that is displayed in the widget header.
        /// </summary>
        [SaveState(null)]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Gets the available output connectors.
        /// </summary>
        public IEnumerable<ConnectorViewModel> OutputConnectors => _outputConnectors;

        /// <summary>
        /// Gets or sets the widget's position (top left).
        /// </summary>
        public Point Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        /// <summary>
        /// Gets or sets the widget's size (width, height).
        /// </summary>
        public Size Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        /// <summary>
        /// Gets the widget's current state that is serialized to the engine.
        /// </summary>
        public virtual ModuleState GetState()
        {
            var state = new ModuleState
            {
                Data =
                {
                    {AppGlobals.ModuleStateKeys.Left, Position.X},
                    {AppGlobals.ModuleStateKeys.Top, Position.Y},
                    {AppGlobals.ModuleStateKeys.Width, Size.Width},
                    {AppGlobals.ModuleStateKeys.Height, Size.Height}
                }
            };

            this.GetStateProperties(state);

            return state;
        }

        /// <summary>
        /// Initializes a widget and loads its state if the state hasn't been loaded yet.
        /// </summary>
        public virtual void Initialize()
        {
            IsInitialized = true;

            if (_loadingWidgetState != null)
            {
                LoadState(_loadingWidgetState);
                _loadingWidgetState = null;
            }
        }

        /// <summary>
        /// Loads the supplied state if the widget is initialized, otherwise loading of the state is deferred to the <see cref="Initialize"/> call.
        /// </summary>
        public void LoadState(ModuleState state)
        {
            if (!IsInitialized)
            {
                _loadingWidgetState = state;
                return;
            }

            SetState(state);
        }

        /// <summary>
        /// Applies the data from the specified state to this instance.
        /// </summary>
        public virtual void SetState(ModuleState state)
        {
            var left = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Left);
            var top = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Top);
            Position = new Point(left, top);

            var width = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Width);
            var height = state.Data.GetObject<double>(AppGlobals.ModuleStateKeys.Height);
            Size = new Size(width, height);

            this.SetStateProperties(state);
        }

        /// <summary>
        /// Creates and adds an input connector with the specified name.
        /// </summary>
        protected ConnectorViewModel AddInput(string name)
        {
            var input = new ConnectorViewModel(name, this);
            AddInput(input);
            return input;
        }

        /// <summary>
        /// Adds an input connector.
        /// </summary>
        protected virtual void AddInput(ConnectorViewModel input)
        {
            _inputConnectors.Add(input);
        }

        /// <summary>
        /// Creates and adds an output connector with the specified name.
        /// </summary>
        protected ConnectorViewModel AddOutput(string name)
        {
            var output = new ConnectorViewModel(name, this);
            AddOutput(output);
            return output;
        }

        /// <summary>
        /// Adds an output connector.
        /// </summary>
        protected virtual void AddOutput(ConnectorViewModel output)
        {
            _outputConnectors.Add(output);
        }
    }
}