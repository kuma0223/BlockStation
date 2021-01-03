using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class MyLoggerProvider : ILoggerProvider
{
    private Task task = null;
    private BlockingCollection<String> queue = new BlockingCollection<string>();

    public ILogger CreateLogger(string categoryName) {
        var rx = new Regex(@"Microsoft\.AspNetCore\.");
        if (rx.IsMatch(categoryName)) {
            return NullLogger.Instance;
        }

        if(task == null) {
            task = new Task(() => WritingAction("D:/logs"));
            task.Start();
        }
        return new MyLogger(this);
    }

    public void Dispose() {
        queue.CompleteAdding();
        if (task != null) {
            task.Wait();
        }
        queue.Dispose();
    }

    /// <summary>
    /// 書き込み登録
    /// </summary>
    private void Offer(string text) {
        queue.TryAdd(text);
    }

    /// <summary>
    /// 書き込みスレッド
    /// </summary>
    private void WritingAction(string dir) {
        StreamWriter writer = null;
        string path = "";

        Action close = () => {
            if (writer != null) {
                writer.Close();
                writer = null;
            }
        };
        Action open = () => {
            var tmp = $"{dir}/{DateTime.Now:yyyyMMdd}.log";
            if(writer == null || path != tmp) {
                close();
                writer = new StreamWriter(tmp, true, Encoding.UTF8);
                path = tmp;
            }
        };

        try {
            while (!queue.IsCompleted) {
                try {
                    string item;
                    if (queue.TryTake(out item, -1)) {
                        open();
                        writer.WriteLine(item);
                        writer.Flush();
                    }
                } catch (Exception) {
                    close();
                }
            }
        } finally {
            close();
        }
    }

    private class MyLogger : ILogger
    {
        private MyLoggerProvider provider;

        public MyLogger(MyLoggerProvider provider) {
            this.provider = provider;
        }

        public IDisposable BeginScope<TState>(TState state) {
            return null;
        }
        public bool IsEnabled(LogLevel logLevel) {
            return true;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            var msg = $"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {formatter(state, exception)}";
            if(exception != null) {
                msg += Environment.NewLine;
                msg += exception.ToString();
            }
            provider.Offer(msg);
        }
    }
}
