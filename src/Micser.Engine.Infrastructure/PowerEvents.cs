using System;
using System.ServiceProcess;

namespace Micser.Engine.Infrastructure
{
    public static class PowerEvents
    {
        public static event EventHandler<PowerBroadcastStatus> PowerStatusChanged;

        public static void OnPowerStatusChanged(PowerBroadcastStatus e)
        {
            PowerStatusChanged?.Invoke(null, e);
        }
    }
}