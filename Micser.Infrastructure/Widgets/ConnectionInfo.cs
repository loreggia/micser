using System;

namespace Micser.Infrastructure.Widgets
{
    public class ConnectionInfo
    {
        public string SinkConnectorName { get; set; }
        public Guid SinkWidgetId { get; set; }
        public string SourceConnectorName { get; set; }
        public Guid SourceWidgetId { get; set; }
    }
}