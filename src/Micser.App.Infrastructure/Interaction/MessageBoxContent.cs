using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    public class MessageBoxContent
    {
        public MessageBoxButton Buttons { get; set; }
        public MessageBoxImage Image { get; set; }
        public bool IsModal { get; set; }
        public string Message { get; set; }
        public Window ParentWindow { get; set; }
    }
}