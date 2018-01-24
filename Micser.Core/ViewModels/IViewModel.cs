using System.ComponentModel;
using System.Threading.Tasks;

namespace Micser.Core.ViewModels
{
    public interface IViewModel : INotifyPropertyChanged
    {
        Task InitializeAsync();
    }
}