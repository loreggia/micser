using System;

namespace Micser.Engine.Infrastructure.Updates
{
    public class UpdateManifest
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string Version { get; set; }
    }
}