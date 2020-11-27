using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Micser.Engine.Infrastructure.Audio;
using Micser.Plugins.Main.Audio;

namespace Micser.Plugins.Main.Modules
{
    public class EqualizerModule : AudioModule
    {
        protected EqualizerModule(ILogger<EqualizerModule> logger)
            : base(logger)
        {
            Filters = new List<FilterDescription>();

            AddSampleProcessor(new EqualizerSampleProcessor(this));
        }

        public List<FilterDescription> Filters { get; }
    }
}