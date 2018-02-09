using System.Windows;
using Micser.Infrastructure.Controls;
using Micser.Infrastructure.ViewModels;
using Micser.Main.ViewModels.Widgets;

namespace Micser.Main.Views.Widgets
{
    public interface IWidgetFactory
    {
        Widget CreateWidget(WidgetViewModel viewModel);
    }
}