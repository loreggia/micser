using System.Collections.Generic;

namespace Micser.Models
{
    public class ModuleDto
    {
        public ModuleDto()
        {
            State = new Dictionary<string, string?>();
        }

        public long Id { get; set; }
        public IDictionary<string, string?> State { get; set; }
        public string? Type { get; set; }
    }
}