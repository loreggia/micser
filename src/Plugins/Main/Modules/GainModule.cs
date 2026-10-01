using System.ComponentModel.DataAnnotations;
using Micser.Audio;

namespace Micser.Plugins.Main.Modules;

/// <param name="Gain">Gain in dB.</param>
public sealed record GainState([Range(-60f, 24f)] float Gain = 0f);

public class GainModule : EffectModule, IStatefulModule<GainState>
{
    private float _appliedFactor = 1f;

    /// <summary>
    /// Gain in dB. Changes are ramped over one block.
    /// </summary>
    public float Gain { get; set; }

    public GainState GetState()
    {
        return new GainState(Gain);
    }

    public void SetState(GainState state)
    {
        Gain = state.Gain;
    }

    protected override void Process(AudioBuffer buffer)
    {
        var factor = Decibels.ToLinear(Gain);
        buffer.ApplyGain(_appliedFactor, factor);
        _appliedFactor = factor;
    }
}
