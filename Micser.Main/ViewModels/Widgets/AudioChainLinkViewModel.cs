using Micser.Infrastructure;
using Micser.Main.Audio;

namespace Micser.Main.ViewModels.Widgets
{
    public class AudioChainLinkViewModel : WidgetViewModel
    {
        protected ConnectorViewModel AddInput(IAudioChainLink link)
        {
            var input = new ConnectorViewModel(this, link);
            input.ConnectionChanged += OnInputConnectionChanged;
            AddInput(input);
            return input;
        }

        protected ConnectorViewModel AddOutput(IAudioChainLink link)
        {
            var output = new ConnectorViewModel(this, link);
            output.ConnectionChanged += OnOutputConnectionChanged;
            AddOutput(output);
            return output;
        }

        protected virtual void OnInputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
        }

        protected virtual void OnOutputConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
        }
    }
}