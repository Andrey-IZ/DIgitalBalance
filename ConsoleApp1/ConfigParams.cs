using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace DigitalBalance
{
    public struct ConfigParams
    {
        public enum KeyParams
        {
            PING_TIMEOUT_IN_SEC,
            SCREEN_TIMEOUT_HOURS_IN_FLOAT,
            UPTIME_MONITOR_IN_MINUTES,
            LAUNCH_ANOTHER_CMD_ON_START,
            PING_HOSTNAME,
            WIFI_INTERFACE_NAME,
        }

        public string WifiInterfaceName;
        public string ContribServicePath;
        public string Hostname;
        public int PingTimeout;
        public float LimitedScreenActiveHours;
        public float MonitorRemainsTimeTimeoutInMinutes;

        public TimeSpan MonitorRemainsTimeout()
        {
            return TimeSpan.FromMinutes(MonitorRemainsTimeTimeoutInMinutes);
        }

        public TimeSpan LimitedScreen()
        {
            return TimeSpan.FromHours(LimitedScreenActiveHours);
        }
    }
}
