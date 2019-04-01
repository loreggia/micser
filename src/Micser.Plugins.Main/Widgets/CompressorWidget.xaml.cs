using Micser.App.Infrastructure;
using System.ComponentModel;
using System.Windows;

namespace Micser.Plugins.Main.Widgets
{
    public partial class CompressorWidget
    {
        public CompressorWidget()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is Bindable old)
            {
                old.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is Bindable b)
            {
                b.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompressorViewModel.EnableAdvancedControls) &&
                sender is CompressorViewModel vm)
            {
                if (!vm.EnableAdvancedControls)
                {
                    Width = 0;
                    Height = 0;
                }
            }
        }
    }
}