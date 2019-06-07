using Micser.Common.Api;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class SpectrumModule : AudioModule
    {
        public SpectrumModule(IApiEndPoint apiEndPoint)
        {
            AddSampleProcessor(new SpectrumSampleProcessor(this, apiEndPoint));
        }
    }
}