namespace Micser.Plugins.Main.Api
{
    public class SpectrumData
    {
        public SpectrumValue[] Values { get; set; }

        public struct SpectrumValue
        {
            public float Frequency { get; set; }
            public float Value { get; set; }
        }
    }
}