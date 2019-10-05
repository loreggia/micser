using Micser.Common;

namespace Micser.App.Infrastructure.ToolBars
{
    /// <summary>
    /// A tool bar subregistry containing its <see cref="ToolBarItem"/>s.
    /// </summary>
    public class ToolBarDescription : ItemRegistry<ToolBarItem>
    {
        /// <summary>
        /// Gets or sets the name of the tool bar.
        /// </summary>
        public string Name { get; set; }
    }
}