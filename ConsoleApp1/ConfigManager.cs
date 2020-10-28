using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DigitalBalance
{
    public class ConfigManager
    {
        private ConfigParams confParams;
        public string ConfigFilePath { get; } = Assembly.GetExecutingAssembly().GetName().Name + ".ini";
        public ConfigParams ConfParams { get => confParams; set => confParams = value; }

        [DllImport("kernel32", CharSet = CharSet.Auto)] // Еще раз подключаем kernel32.dll, а теперь описываем функцию GetPrivateProfileString
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32")] // Подключаем kernel32.dll и описываем его функцию WritePrivateProfilesString
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        public ConfigManager()
        {
            ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFilePath);
            ConfParams = new ConfigParams
            {
                Hostname = "www.yandex.ru",
                LimitedScreenActiveHours = 6,
                PingTimeout = 5,
                MonitorRemainsTimeTimeoutInMinutes = 1,
                ContribServicePath = null,
            };
            
            try
            {
                confParams.WifiInterfaceName = NetworkInterface.GetAllNetworkInterfaces().
                                    Where(i => i.OperationalStatus == OperationalStatus.Up &&
                                                (i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                                                || i.NetworkInterfaceType == NetworkInterfaceType.Ethernet)).
                                    FirstOrDefault().Name;
            }
            catch (Exception)
            {
                confParams.WifiInterfaceName = null;
            }
        }

        /// <summary>
        /// Читаем ini-файл и возвращаем значение указного ключа из заданной секции.
        /// </summary>
        private string ReadINI(string Section, string Key)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", RetVal, 255, ConfigFilePath);
            return RetVal.ToString();
        }

        /// <summary>
        /// Записываем в ini-файл. Запись происходит в выбранную секцию в выбранный ключ.
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public void WriteINI(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, ConfigFilePath);
        }

        /// <summary>
        /// читаем в массив все пары ключей-значнией в данной секции
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="fileName"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private bool GetPrivateProfileSection(string appName, string fileName, out string[] section)
        {
            section = null;

            if (!System.IO.File.Exists(fileName))
                return false;

            const uint MAX_BUFFER = 32767;

            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER * sizeof(char));

            uint bytesReturned = GetPrivateProfileSection(appName, pReturnedString, MAX_BUFFER, fileName);

            if ((bytesReturned == MAX_BUFFER - 2) || (bytesReturned == 0))
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return false;
            }
            string returnedString = Marshal.PtrToStringAuto(pReturnedString, (int)(bytesReturned - 1));

            section = returnedString.Split('\0');

            Marshal.FreeCoTaskMem(pReturnedString);
            return true;
        }

        /// <summary>
        /// Загрузить данные из конфига
        /// </summary>
        public bool LoadParams()
        {
            if (!File.Exists(ConfigFilePath))
                return false;

            var sectionDefault = "Default";

            var pingTimeout = ReadINI(sectionDefault, ConfigParams.KeyParams.PING_TIMEOUT_IN_SEC.ToString());
            if (!string.IsNullOrEmpty(pingTimeout))
            {
                int.TryParse(pingTimeout, out confParams.PingTimeout);
            }

            var screenTimeout = ReadINI(sectionDefault, ConfigParams.KeyParams.SCREEN_TIMEOUT_HOURS_IN_FLOAT.ToString());
            if (!string.IsNullOrEmpty(screenTimeout))
            {
                float.TryParse(screenTimeout, out confParams.LimitedScreenActiveHours);
            }

            var remainsMinutesTimeout = ReadINI(sectionDefault, ConfigParams.KeyParams.UPTIME_MONITOR_IN_MINUTES.ToString());
            if (!string.IsNullOrEmpty(remainsMinutesTimeout))
            {
                float.TryParse(remainsMinutesTimeout, out confParams.MonitorRemainsTimeTimeoutInMinutes);
            }

            var externServicePath = ReadINI(sectionDefault, ConfigParams.KeyParams.LAUNCH_ANOTHER_CMD_ON_START.ToString());
            if (!string.IsNullOrEmpty(externServicePath))
            {
                confParams.ContribServicePath = externServicePath;
            }

            var pingHostname = ReadINI(sectionDefault, ConfigParams.KeyParams.PING_HOSTNAME.ToString());
            if (!string.IsNullOrEmpty(pingHostname))
            {
                confParams.Hostname = pingHostname;
            }

            var interfaceName = ReadINI(sectionDefault, ConfigParams.KeyParams.WIFI_INTERFACE_NAME.ToString());
            if(!string.IsNullOrEmpty(interfaceName))
            {
                confParams.WifiInterfaceName = interfaceName;
            }

            return true;
        }

        public void SaveParams()
        {
            var section = "Default";
            foreach (var key in Enum.GetValues(typeof(ConfigParams.KeyParams)))
            {
                string value = "";
                switch ((ConfigParams.KeyParams)key)
                {
                    case ConfigParams.KeyParams.PING_TIMEOUT_IN_SEC:
                        value = confParams.PingTimeout.ToString();
                        break;
                    case ConfigParams.KeyParams.SCREEN_TIMEOUT_HOURS_IN_FLOAT:
                        value = confParams.LimitedScreenActiveHours.ToString();
                        break;
                    case ConfigParams.KeyParams.UPTIME_MONITOR_IN_MINUTES:
                        value = confParams.MonitorRemainsTimeTimeoutInMinutes.ToString();
                        break;
                    case ConfigParams.KeyParams.LAUNCH_ANOTHER_CMD_ON_START:
                        value = confParams.ContribServicePath;
                        break;
                    case ConfigParams.KeyParams.PING_HOSTNAME:
                        value = confParams.Hostname;
                        break;
                    case ConfigParams.KeyParams.WIFI_INTERFACE_NAME:
                        value = confParams.WifiInterfaceName;
                        break;
                }
                WriteINI(section, key.ToString(), value);
            }
        }
    }
}
