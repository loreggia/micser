using System;
using System.Collections.Generic;
using System.Windows;

namespace Micser.Infrastructure.Widgets
{
    public class WidgetState
    {
        public WidgetState()
        {
            Settings = new Dictionary<string, object>();
        }

        public Guid Id { get; set; }
        public IEnumerable<Guid> InputConnectors { get; set; }
        public string Name { get; set; }
        public IEnumerable<Guid> OutputConnectors { get; set; }
        public Point Position { get; set; }
        public IDictionary<string, object> Settings { get; set; }
        public Type ViewModelType { get; set; }
    }
}