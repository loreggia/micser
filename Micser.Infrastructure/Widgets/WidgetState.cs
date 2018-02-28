using System;
using System.Collections.Generic;
using System.Windows;

namespace Micser.Infrastructure.Widgets
{
    public class WidgetState
    {
        public WidgetState()
        {
            Connections = new Dictionary<Guid, Guid>();
            Settings = new Dictionary<string, object>();
        }

        public IDictionary<Guid, Guid> Connections { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Point Position { get; set; }
        public IDictionary<string, object> Settings { get; set; }
        public Type ViewModelType { get; set; }
    }
}