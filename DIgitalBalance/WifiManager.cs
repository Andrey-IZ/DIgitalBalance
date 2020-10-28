using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DigitalBalance
{
    public static class WifiManager
    {
        public static void EnableAdapter(string interfaceName)
        {
            WiFiSwitcher(interfaceName, true);
        }

        public static void DisableAdapter(string interfaceName)
        {
            WiFiSwitcher(interfaceName, false);
        }

        private static void WiFiSwitcher(string interfaceName, bool isEnable)
        {
            string option = isEnable ? "enable" : "disable";
            ProcessStartInfo psi = new ProcessStartInfo("netsh", "interface set interface \"" + interfaceName + "\" " + option);
            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();
            }
        }
    }
}
