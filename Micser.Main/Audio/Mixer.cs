using NAudio.Wave.SampleProviders;

namespace Micser.Main.Audio
{
    public class Mixer : AudioChainLink
    {
        private readonly AudioChainLinkSampleProvider _input1SampleProvider;
        private readonly AudioChainLinkSampleProvider _input2SampleProvider;
        private readonly MixingSampleProvider _sampleProvider;

        public Mixer()
        {
            _input1SampleProvider = new AudioChainLinkSampleProvider();
            _input2SampleProvider = new AudioChainLinkSampleProvider();
            _sampleProvider = new MixingSampleProvider(new[] { _input1SampleProvider, _input2SampleProvider });
        }

        public IAudioChainLink Input2 { get; set; }

        protected override int ReadInternal(float[] buffer, int offset, int count)
        {
            _input1SampleProvider.Input = Input;
            _input2SampleProvider.Input = Input2;

            return _sampleProvider.Read(buffer, offset, count);
        }
    }
}