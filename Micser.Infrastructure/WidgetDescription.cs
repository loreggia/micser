using System;

namespace Micser.Infrastructure
{
    public class WidgetDescription
    {
        public WidgetDescription()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }
    }
}