using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class CompressorModule : AudioModule
    {
        public CompressorModule(long id)
            : base(id)
        {
            AddSampleProcessor(new CompressorSampleProcessor());
        }
    }
}