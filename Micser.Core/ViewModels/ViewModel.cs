using System.Threading.Tasks;
using Micser.Core.Common;

namespace Micser.Core.ViewModels
{
    public class ViewModel : Bindable, IViewModel
    {
        public virtual async Task InitializeAsync()
        {
            await Task.Yield();
        }
    }
}