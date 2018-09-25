using Newtonsoft.Json;
using System;

namespace Micser.Shared.Models
{
    public class AudioModuleDescription : Model
    {
        public string State { get; set; }
        public Type Type { get; set; }

        public T GetState<T>()
        {
            if (string.IsNullOrEmpty(State))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(State);
        }

        public void SetState(object state)
        {
            State = state != null ? JsonConvert.SerializeObject(state) : null;
        }
    }
}