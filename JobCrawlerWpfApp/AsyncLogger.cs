using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace JobCrawlerWpfApp
{


    public static class AsyncLogger
    {
        private static readonly BlockingCollection<string> _logQueue = new BlockingCollection<string>();
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static Task _loggingTask;

        static AsyncLogger()
        {
            // Start the background logging thread
            _loggingTask = Task.Run(() => ProcessLogQueue(_cts.Token));
        }

        public static void Initialize(string logFilePath)
        {
            LogFilePath = logFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        }

        public static string LogFilePath { get; private set; }

        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _logQueue.Add($"[{timestamp}] {message}");
        }

        public static void Shutdown()
        {
            _logQueue.CompleteAdding();
            _cts.Cancel();
            _loggingTask?.Wait();
        }

        private static void ProcessLogQueue(CancellationToken ct)
        {
            try
            {
                using (var writer = new StreamWriter(LogFilePath, append: true))
                {
                    writer.AutoFlush = true;

                    foreach (var message in _logQueue.GetConsumingEnumerable(ct))
                    {
                        writer.WriteLine(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if logging fails
                Console.WriteLine($"Logger failed: {ex}");
            }
        }
    }
}
