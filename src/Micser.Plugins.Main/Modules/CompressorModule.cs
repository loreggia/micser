using Micser.Common.Modules;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class CompressorModule : AudioModule
    {
        public CompressorModule(long id)
            : base(id)
        {
            AddSampleProcessor(new CompressorSampleProcessor(this));
        }

        public float Amount { get; set; }
        public float Ratio { get; set; }
        public float Threshold { get; set; }
        public CompressorType Type { get; set; }

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[StateKeys.Amount] = Amount;
            state.Data[StateKeys.Ratio] = Ratio;
            state.Data[StateKeys.Threshold] = Threshold;
            state.Data[StateKeys.Type] = Type;
            return state;
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            Amount = state.Data.GetObject(StateKeys.Amount, 1f);
            Ratio = state.Data.GetObject(StateKeys.Amount, 2f);
            Threshold = state.Data.GetObject(StateKeys.Amount, 0f);
            Type = state.Data.GetObject(StateKeys.Amount, CompressorType.Upward);
        }

        public class StateKeys
        {
            public const string Amount = "Amount";
            public const string Ratio = "Ratio";
            public const string Threshold = "Threshold";
            public const string Type = "Type";
        }
    }
}