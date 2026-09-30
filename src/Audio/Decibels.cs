namespace Micser.Audio;

public static class Decibels
{
    public static float FromLinear(float linear)
    {
        return 20f * MathF.Log10(linear);
    }

    public static float ToLinear(float decibels)
    {
        return MathF.Pow(10f, decibels / 20f);
    }
}
