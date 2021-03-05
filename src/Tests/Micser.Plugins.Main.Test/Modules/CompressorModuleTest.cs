using Microsoft.Extensions.Logging;
using Micser.Plugins.Main.Audio;
using Micser.Plugins.Main.Modules;
using Micser.TestCommon;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Plugins.Main.Test.Modules
{
    public class CompressorModuleTest
    {
        private readonly ILogger<CompressorModule> _logger;

        public CompressorModuleTest(ITestOutputHelper testOutputHelper)
        {
            _logger = new TestLogger<CompressorModule>(testOutputHelper);
        }

        [Fact]
        public void GetState()
        {
            var module = new CompressorModule(_logger);

            var state = module.GetState();
        }

        [Fact]
        public void HasSampleProcessor()
        {
            var module = new CompressorModule(_logger);

            Assert.Contains(module.SampleProcessors, p => p is CompressorSampleProcessor);
        }

        [Fact]
        public void ValidateDefaults()
        {
            var module = new CompressorModule(_logger);

            Assert.Equal(CompressorModule.Defaults.Amount, module.Amount);
            Assert.Equal(CompressorModule.Defaults.Attack, module.Attack);
            Assert.Equal(CompressorModule.Defaults.Knee, module.Knee);
            Assert.Equal(CompressorModule.Defaults.MakeUpGain, module.MakeUpGain);
            Assert.Equal(CompressorModule.Defaults.Ratio, module.Ratio);
            Assert.Equal(CompressorModule.Defaults.Release, module.Release);
            Assert.Equal(CompressorModule.Defaults.Threshold, module.Threshold);
            Assert.Equal(CompressorModule.Defaults.Type, module.Type);
        }
    }
}