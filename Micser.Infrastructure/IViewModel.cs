using System.ComponentModel;
using Prism.Regions;

namespace Micser.Infrastructure
{
    public interface IViewModel : INavigationAware, INotifyPropertyChanged
    {
    }
}