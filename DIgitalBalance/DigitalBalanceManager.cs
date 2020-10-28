using DigitalBalance;
using System.Net.NetworkInformation;
using System.Linq;
using System.Timers;

namespace DigitalBalance
{
    class DigitalBalanceManager : System.IDisposable
    {
        Logger logger = new Logger();
        ConfigManager configManager = new ConfigManager();
        UptimeMonitor uptimeMonitor = new UptimeMonitor();
        Ping pingSender = new Ping();
        Timer pingTimer = new Timer();
        public bool HasInternetAccess { get; set; } = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        public void Start()
        {
            logger.Info(message: "\nService has been started");
            logger.Info(string.Format("Поиск сетевых интерфейсов: [{0}]",
                string.Join(", ",
                    NetworkInterface.GetAllNetworkInterfaces().
                        Where(i => i.OperationalStatus == OperationalStatus.Up &&
                                   i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211).
                        Select(i => i.Name).ToArray())));

            logger.Info($"Загружаем параметры конфигурации из {configManager.ConfigFilePath}");
            if (!configManager.LoadParams())
            {
                logger.Warn($"Конфиг не обнаружен. Создаём новый по умолчанию: {configManager.ConfigFilePath}");
                configManager.SaveParams();
            }

            pingTimer.Elapsed += pingTestEventHandler;
            pingTimer.Interval = configManager.ConfParams.PingTimeout * 1000;
            pingTimer.Start();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        private void pingTestEventHandler(object sender, ElapsedEventArgs e)
        {
            (sender as Timer).Stop();
            bool pingStatus = false;
            try
            {
                var pingResult = pingSender.Send(configManager.ConfParams.Hostname,
                                (configManager.ConfParams.PingTimeout) * 1000);
                pingStatus = pingResult.Status == IPStatus.Success;
                logger.Info($"ip={pingResult.Address.ToString()} [{configManager.ConfParams.Hostname}], ttl={pingResult.Options.Ttl}");
            }
            catch (System.Exception)
            {
                pingStatus = false;
            }
            
            logger.Info($"pingStatus = {pingStatus}, HasInternetAccess={HasInternetAccess}");
            if (pingStatus != HasInternetAccess)
            {
                logger.Warn(string.Format("Интернет {0} [осталось: {1}]",
                    pingStatus ? "включен" : "отключен",
                    uptimeMonitor.CountDown));

                HasInternetAccess = pingStatus;
                logger.Info($"HasInternetAccess = {HasInternetAccess}");
                if (HasInternetAccess)
                {
                    if (uptimeMonitor.IsTimeUp) // интернет есть, но время для интернета вышло
                    {
                        logger.Warn($"С тебя хватит интернета на сегодня! Вырубаем интернет на {configManager.ConfParams.WifiInterfaceName}");
                        WifiManager.DisableAdapter(configManager.ConfParams.WifiInterfaceName);
                    }
                    else  // интернет есть, включаем счетчик времени
                    {
                        logger.Info($"Стартуем монитор активности: суточная блокировка = {configManager.ConfParams.LimitedScreen()}, " +
                            $"частота проверки активности = {configManager.ConfParams.MonitorRemainsTimeout()}");
                        logger.Info($"Используем кеш {uptimeMonitor.CacheFilePath}");

                        uptimeMonitor.StartMonitor(configManager.ConfParams.LimitedScreen(),
                                                   configManager.ConfParams.MonitorRemainsTimeout());
                    }
                }
                else  // нет интернета, значит остановить мониторинг времени активности?
                {
                    logger.Warn("Остановить монитор активности");
                    //uptimeMonitor.StopMonitor();
                }
            }
            (sender as Timer).Start();
        }

        public void Stop()
        {
            logger.Info(message: "Service has been stopped");
            pingTimer.Stop();
            pingSender.Dispose();
            uptimeMonitor.StopMonitor();
        }

        public void Dispose()
        {
        }
    }
}
