using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Micser.App.Infrastructure
{
    public class Bindable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T store, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(store, newValue))
            {
                store = newValue;
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }
    }
}