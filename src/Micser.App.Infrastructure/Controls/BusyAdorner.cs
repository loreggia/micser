using System.Windows;
using System.Windows.Documents;

namespace Micser.App.Infrastructure.Controls
{
    public class BusyAdorner : Adorner
    {
        public BusyAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }
    }
}