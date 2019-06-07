using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;
using System.Collections.Generic;

namespace Micser.Plugins.Main.Modules
{
    public class EqualizerModule : AudioModule
    {
        protected EqualizerModule()
        {
            Filters = new List<FilterDescription>();

            AddSampleProcessor(new EqualizerSampleProcessor(this));
        }

        public List<FilterDescription> Filters { get; }
    }
}