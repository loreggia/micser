using Micser.App.Infrastructure.Widgets;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class CompressorViewModel : AudioWidgetViewModel
    {
        public CompressorViewModel()
        {
            AddInput("Input1");
            AddOutput("Output1");
        }

        public override Type ModuleType => typeof(CompressorModule);
    }
}