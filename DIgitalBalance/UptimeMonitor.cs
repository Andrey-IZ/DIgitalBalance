using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;

namespace DigitalBalance
{
    /// <summary>
    /// Упрощает работу с контролированием оставшихся часов активности
    /// </summary>
    public class UptimeMonitor: IDisposable
    {
        public DateTime TodayDate { get; } = DateTime.Today.Date;
        public string CacheFilePath { get; } = Assembly.GetExecutingAssembly().GetName().Name + ".cache";
        public TimeSpan MonitorTimeout { get; private set; } 
        public bool IsRunning { get; private set; } = false;
        public TimeSpan CountDown { get; private set; }
        public event EventHandler TimeIsOver;
        public bool IsTimeUp { get; private set; } = false;

        private readonly Timer timer = new Timer();
        // Будем держать дескриптор открытым пока работает сервис
        private FileStream fileStream = null;   

        private struct DataCache
        {
            public DateTime TodayDate;
            public TimeSpan RemainedTime;
        }

        public UptimeMonitor()
        {
            CacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CacheFilePath);
        }

        public void StartMonitor(TimeSpan uptimeInitValue ,TimeSpan monitorTimeout)
        {
            if (!IsRunning)
            {
                CountDown = uptimeInitValue;
                MonitorTimeout = monitorTimeout;
                timer.Interval = MonitorTimeout.TotalSeconds * 1000;
                timer.Elapsed += Timer_Elapsed;
                IsRunning = true;
                OpenCacheFile();
                timer.Start();
            }
        }

        public void StopMonitor()
        {
            timer.Stop();
            IsRunning = false;
            Dispose();
        }

        /// <summary>
        /// Таймер сервиса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            (sender as Timer).Stop();
            if (IsTimeUp) IsTimeUp = false;

            // Прочитать кэш
            DataCache data = ReadDataCache();

            // Файл был создан сегодня до полуночи
            if (data.TodayDate.CompareTo(TodayDate) == 0 && CountDown >= data.RemainedTime)
            {
                // Вычитаем интервал таймера + 1000 тиков.
                CountDown = data.RemainedTime.Subtract(MonitorTimeout.Add(new TimeSpan(1000)));
                // Если ещё осталось время активности
                if (CountDown.TotalSeconds > timer.Interval*0.001)
                {
                    // Обновить кэш
                    data.RemainedTime = CountDown;
                    WriteDataCache(data);
                }
                else  // Всё, время вышло
                {
                    IsTimeUp = true;
                    // Посылаем сигнал
                    TimeIsOver?.Invoke(this, new EventArgs());
                    StopMonitor();
                }
            }
            else  // Если наступил новый день
            {
                IsTimeUp = false;
                // Обнуляем счётчики
                WriteDataCache(newDefaultDataCache());
            }
            (sender as Timer).Start();
        }

        private DataCache newDefaultDataCache()
        {
            return new DataCache
            {
                TodayDate = TodayDate,
                RemainedTime = CountDown
            };
        }

        /// <summary>
        /// Затираем метаданные кэша
        /// </summary>
        /// <param name="data"></param>
        private void WriteDataCache(DataCache data)
        {
            byte[] buffer = getBytes(data);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Считываем метаданные
        /// </summary>
        /// <returns></returns>
        private DataCache ReadDataCache()
        {
            DataCache data = new DataCache();
            byte[] buffer = getBytes(data);
            fileStream.Seek(0, SeekOrigin.Begin);
            var bytesRead = fileStream.Read(buffer, 0, buffer.Length);

            if (buffer.Length > bytesRead)
                return data;

            return fromBytes(buffer);
        }

        byte[] getBytes(DataCache data)
        {
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        DataCache fromBytes(byte[] arr)
        {
            DataCache data = new DataCache();

            int size = Marshal.SizeOf(data);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            data = (DataCache)Marshal.PtrToStructure(ptr, data.GetType());
            Marshal.FreeHGlobal(ptr);

            return data;
        }

        /// <summary>
        /// Открываем или создаём файл
        /// </summary>
        private void OpenCacheFile()
        {
            if (!File.Exists(CacheFilePath))
            {
                DataCache data = new DataCache();
                int bufSize = Marshal.SizeOf(data);
                File.Create(CacheFilePath, bufSize, FileOptions.SequentialScan);
            }
            else
            {
                fileStream = File.Open(CacheFilePath, FileMode.Open);
            }
        }

        private void CloseCacheFile()
        {
            fileStream.Flush();
            fileStream.Close();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    CloseCacheFile();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~UptimeMonitor()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
