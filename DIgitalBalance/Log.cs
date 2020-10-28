using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DigitalBalance
{
    public class Logger
    {
        private readonly string path;
        public bool LogStarted { get; private set; } = false;

        public Logger(string path = null)
        {
            if (path == null)
                this.path =Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        }

        public void Info(string message)
        {
            WriteToFile($"[INFO] {message}");
        }

        public void Warn(string message)
        {
            WriteToFile($"[WARN] {message}");
        }

        private void WriteToFile(string message)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filepath = Path.Combine(path, "ServiceLog_" + DateToString().Replace('/', '_') + ".txt");

            if (File.Exists(filepath))
            {
                using (var fs = File.AppendText(filepath))
                {
                    if (!LogStarted)
                        LogStarted = true;
                    WriteMessage(message, fs);
                }
            }
            else
            {
                using (var fs = File.CreateText(filepath))
                {
                    WriteMessage(message, fs);
                }
            }
        }

        private static void WriteMessage(string message, StreamWriter fs)
        {
            fs.WriteLine($"{TimeLogString()}: {message}");
        }

        private static string DateToString()
        {
            return DateTime.Now.Date.ToShortDateString();
        }

        private static string TimeLogString()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff");
        }
    }
}
