using System;

namespace Micser.Infrastructure
{
    public class WidgetDescription
    {
        public string Name { get; set; }
        public Type ViewModelType { get; set; }
        public Type ViewType { get; set; }
    }
}