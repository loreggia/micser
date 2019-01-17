using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Micser.Common.DataAccess
{
    public class DbSet<T> : ObservableCollection<T>
    {
        public bool HasChanges { get; protected set; }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            HasChanges = true;
        }
    }
}