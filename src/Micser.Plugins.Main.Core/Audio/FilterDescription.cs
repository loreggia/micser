namespace Micser.Plugins.Main.Audio
{
    public class FilterDescription
    {
        public FilterDescription()
        {
            Frequency = 1000;
            BandWidth = 18;
            PeakGainDb = 0;
            Ratio = 1;
        }

        public double BandWidth { get; set; }
        public double Frequency { get; set; }
        public double PeakGainDb { get; set; }
        public double Ratio { get; set; }
    }
}