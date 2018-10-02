using Micser.Infrastructure.Widgets;

namespace Micser.Plugins.Main.ViewModels.Widgets
{
    public class AudioChainLinkViewModel : WidgetViewModel
    {
        protected ConnectorViewModel AddInput(string name)
        {
            var input = new ConnectorViewModel(name, this, null);
            input.ConnectionChanged += OnInputConnectionChanged;
            AddInput(input);
            return input;
        }

        protected ConnectorViewModel AddOutput(string name)
        {
            var output = new ConnectorViewModel(name, this, null);
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