using System.Linq;
using CSCore;
using Microsoft.Extensions.Logging;
using Micser.Common.Audio;
using Micser.TestCommon;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Audio
{
    public class AudioModuleTest
    {
        private readonly ILogger _logger;

        public AudioModuleTest(ITestOutputHelper testOutputHelper)
        {
            _logger = new TestLogger<AudioModuleTest>(testOutputHelper);
        }

        [Fact]
        public void HasVolumeSampleProcessor()
        {
            var module = new TestModule(_logger);

            var sampleProcessors = module.SampleProcessors.ToArray();

            Assert.Single(sampleProcessors);
            Assert.IsType<VolumeSampleProcessor>(sampleProcessors[0]);
        }

        [Fact]
        public void SampleProcessorsAreSorted()
        {
            var module = new TestModule(_logger);
            module.AddSampleProcessor(new VolumeSampleProcessor(module) { Priority = -100 });
            module.AddSampleProcessor(new VolumeSampleProcessor(module) { Priority = 100 });

            var sampleProcessors = module.SampleProcessors.ToArray();
            Assert.Collection(
                sampleProcessors,
                p1 => Assert.Equal(int.MaxValue, p1.Priority),
                p2 => Assert.Equal(100, p2.Priority),
                p3 => Assert.Equal(-100, p3.Priority));
        }

        [Fact]
        public void WriteSample_FullVolume_PassThrough()
        {
            var source = new TestModule(_logger) { IsEnabled = true };
            var output = new TestModule(_logger) { IsEnabled = true };
            source.AddOutput(output);

            source.Volume = 1f;

            var waveFormat = new WaveFormat();
            var samples = new[] { 0f, 1f };

            source.WriteSample(source, waveFormat, samples);

            Assert.Same(samples, output.InputChannelSamples);
        }

        [Fact]
        public void WriteSample_IsDisabled_PassThrough()
        {
            var source = new TestModule(_logger) { IsEnabled = false };
            var output = new TestModule(_logger) { IsEnabled = true };
            source.AddOutput(output);

            source.Volume = 0f;

            var waveFormat = new WaveFormat();
            var samples = new[] { 0.5f, 1f };

            source.WriteSample(source, waveFormat, samples);

            Assert.Same(samples, output.InputChannelSamples);
        }

        [Fact]
        public void WriteSample_IsMuted_NoOutput()
        {
            var source = new TestModule(_logger) { IsEnabled = true, IsMuted = true };
            var output = new TestModule(_logger) { IsEnabled = true };
            source.AddOutput(output);

            var waveFormat = new WaveFormat();
            var samples = new[] { 0.5f, 1f };

            source.WriteSample(source, waveFormat, samples);

            Assert.Null(output.InputChannelSamples);
        }

        [Fact]
        public void WriteSample_Volume()
        {
            var source = new TestModule(_logger) { IsEnabled = true };
            var output = new TestModule(_logger) { IsEnabled = true };
            source.AddOutput(output);

            source.Volume = 0.5f;

            var waveFormat = new WaveFormat();
            var samples = new[] { 0.5f, 1f };

            source.WriteSample(source, waveFormat, samples);

            Assert.Equal(new[] { 0.25f, 0.5f }, output.InputChannelSamples);
        }

        [Fact]
        public void WriteSample_ZeroVolume_NoOutput()
        {
            var source = new TestModule(_logger) { IsEnabled = true };
            var output = new TestModule(_logger) { IsEnabled = true };
            source.AddOutput(output);

            source.Volume = 0f;

            var waveFormat = new WaveFormat();
            var samples = new[] { 0.5f, 1f };

            source.WriteSample(source, waveFormat, samples);

            Assert.Null(output.InputChannelSamples);
        }

        private class TestModule : AudioModule
        {
            public TestModule(ILogger logger)
                : base(logger)
            {
            }

            public float[]? InputChannelSamples { get; private set; }
            public float[]? OutputChannelSamples { get; private set; }

            public override void WriteSample(IAudioModule source, WaveFormat waveFormat, float[] channelSamples)
            {
                base.WriteSample(source, waveFormat, channelSamples);
                InputChannelSamples = channelSamples;
                OutputChannelSamples = ChannelSamplesBuffer;
            }
        }
    }
}