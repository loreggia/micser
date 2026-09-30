using Micser.Audio;

namespace Micser.Plugins.Main.Modules;

public class GainModule : EffectModule
{
    private float _appliedFactor = 1f;

    /// <summary>
    /// Gain in dB. Changes are ramped over one block.
    /// </summary>
    public float Gain { get; set; }

    protected override void Process(AudioBuffer buffer)
    {
        var factor = Decibels.ToLinear(Gain);
        buffer.ApplyGain(_appliedFactor, factor);
        _appliedFactor = factor;
    }
}
