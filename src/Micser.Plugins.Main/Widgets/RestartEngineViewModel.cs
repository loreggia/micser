using Micser.App.Infrastructure.Widgets;
using Micser.Common.Modules;
using Micser.Plugins.Main.Modules;
using System;

namespace Micser.Plugins.Main.Widgets
{
    public class RestartEngineViewModel : WidgetViewModel
    {
        private float _delay;

        [SaveState(5f)]
        public float Delay
        {
            get => _delay;
            set => SetProperty(ref _delay, value);
        }

        public override Type ModuleType => typeof(RestartEngineModule);

        public override ModuleState GetState()
        {
            return base.GetState();
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);
        }
    }
}