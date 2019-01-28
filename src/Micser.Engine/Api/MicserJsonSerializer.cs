using Newtonsoft.Json;

namespace Micser.Engine.Api
{
    public sealed class MicserJsonSerializer : JsonSerializer
    {
        public MicserJsonSerializer()
        {
#if DEBUG
            Formatting = Formatting.Indented;
#else
            Formatting = Formatting.None;
#endif
        }
    }
}