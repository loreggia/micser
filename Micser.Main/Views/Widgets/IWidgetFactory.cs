using System.Windows;
using Micser.Main.Controls;
using Micser.Main.ViewModels.Widgets;

namespace Micser.Main.Views.Widgets
{
    public interface IWidgetFactory
    {
        Widget CreateWidget(WidgetViewModel viewModel);
    }
}