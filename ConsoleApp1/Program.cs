using DigitalBalance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Timers;

namespace ConsoleApp1
{
    class Program
    {
        private static Ping pingSender = new Ping();

        static void Main(string[] args)
        {
            //Timer timer = new Timer();
            //timer.Elapsed += Timer_Elapsed;
            //timer.Interval = 100;
            //timer.Start();
            DigitalBalanceManager service = new DigitalBalanceManager();
            service.Start();

            Console.ReadLine();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            (sender as Timer).Stop();
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff")+ " ping:");
            var pingResult = pingSender.Send("www.yandex.ru", 5000);
            if (pingResult.Status == IPStatus.Success)
            {
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " ping OK");
            }
            else
            {
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " NO");
            }
            (sender as Timer).Start();
        }
    }
}
