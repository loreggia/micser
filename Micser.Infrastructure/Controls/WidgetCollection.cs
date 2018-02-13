using System.Collections;
using System.Windows.Data;

namespace Micser.Infrastructure.Controls
{
    public class WidgetCollection : CollectionView
    {
        public WidgetCollection(IEnumerable collection)
            : base(collection)
        {
        }
    }
}