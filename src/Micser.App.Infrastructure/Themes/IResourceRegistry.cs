using Micser.Common;
using System.Windows;

namespace Micser.App.Infrastructure.Themes
{
    /// <summary>
    /// Registry containing WPF resource dictionaries from all modules.
    /// </summary>
    public interface IResourceRegistry : IItemRegistry<ResourceDictionary>
    {
    }
}