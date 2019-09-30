using System.Windows.Input;

namespace Micser.App.Infrastructure.ToolBars
{
    /// <summary>
    /// Describes a tool bar button that executes an <see cref="ICommand"/> when clicked.
    /// </summary>
    public class ToolBarButton : ToolBarItem
    {
        /// <summary>
        /// Gets or sets a command that is executed when the button is clicked.
        /// </summary>
        public ICommand Command { get; set; }
    }
}