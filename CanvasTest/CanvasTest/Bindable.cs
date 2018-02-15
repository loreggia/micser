using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CanvasTest
{
    public class Bindable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T store, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(store, value))
            {
                store = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }
    }
}