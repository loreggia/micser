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
        public float Attack { get; set; }
        public float Ratio { get; set; }
        public float Release { get; set; }
        public float Threshold { get; set; }
        public CompressorType Type { get; set; }

        public override ModuleState GetState()
        {
            var state = base.GetState();
            state.Data[StateKeys.Amount] = Amount;
            state.Data[StateKeys.Attack] = Attack;
            state.Data[StateKeys.Ratio] = Ratio;
            state.Data[StateKeys.Release] = Release;
            state.Data[StateKeys.Threshold] = Threshold;
            state.Data[StateKeys.Type] = Type;
            return state;
        }

        public override void SetState(ModuleState state)
        {
            base.SetState(state);

            Amount = state.Data.GetObject(StateKeys.Amount, Defaults.Amount);
            Attack = state.Data.GetObject(StateKeys.Attack, Defaults.Attack);
            Ratio = state.Data.GetObject(StateKeys.Ratio, Defaults.Ratio);
            Release = state.Data.GetObject(StateKeys.Release, Defaults.Release);
            Threshold = state.Data.GetObject(StateKeys.Threshold, Defaults.Threshold);
            Type = state.Data.GetObject(StateKeys.Type, Defaults.Type);
        }

        public class Defaults
        {
            public const float Amount = 1f;
            public const float Attack = 1f;
            public const float Ratio = 2f;
            public const float Release = 10f;
            public const float Threshold = 0.5f;
            public const CompressorType Type = CompressorType.Upward;
        }

        public class StateKeys
        {
            public const string Amount = "Amount";
            public const string Attack = "Attack";
            public const string Ratio = "Ratio";
            public const string Release = "Release";
            public const string Threshold = "Threshold";
            public const string Type = "Type";
        }
    }
}