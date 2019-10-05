using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Base class that implements <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public abstract class Bindable : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when a bindable property value changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets a property backing field and calls <see cref="OnPropertyChanged"/> if the new value differs from the old one.
        /// </summary>
        /// <example>
        /// private string _myProperty;
        /// public string MyProperty { get => _myProperty; set => SetProperty(ref _myProperty, value); }
        /// </example>
        /// <typeparam name="T">The type of the current property.</typeparam>
        /// <param name="store">The property backing field.</param>
        /// <param name="newValue">The property value variable.</param>
        /// <param name="propertyName">Optional override of the property name. Omit this to use the current property's name.</param>
        /// <returns>True, if the property was changed and <see cref="OnPropertyChanged"/> was fired, otherwise false.</returns>
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