using Newtonsoft.Json;
using System;
using System.Windows;

namespace Micser.Common.Widgets
{
    [Serializable]
    public sealed class WidgetState
    {
        public WidgetState()
        {
            Data = new StateDictionary();

            Volume = 1f;
        }

        [JsonExtensionData]
        public StateDictionary Data { get; set; }

        public bool IsMuted { get; set; }
        public string Name { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
        public float Volume { get; set; }
    }
}