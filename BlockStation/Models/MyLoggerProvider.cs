using BlockStation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// ファイル出力ロガープロバイダー
/// </summary>
public class MyLoggerProvider : ILoggerProvider
{
    private Task task = null;
    private BlockingCollection<String> queue = new BlockingCollection<string>();
    
    private string root;
    private Dictionary<string, LogLevel> levels = new Dictionary<string, LogLevel>();
    private LogLevel defaultLevel = LogLevel.Information;

    //----------

    public MyLoggerProvider(IConfigurationSection configration) {
        root = configration.GetValue<string>("RootDirectory");

        foreach(var x in configration.GetSection("LogLevel").GetChildren()) {
            var lvl = Enum.Parse<LogLevel>(x.Value);
            if(x.Key == "Default"){
                defaultLevel = lvl;
            } else {
                levels[x.Key] = lvl;
            }
        }

        task = new Task(WritingAction);
        task.Start();
    }

    public void Dispose() {
        queue.CompleteAdding();
        if (task != null) {
            task.Wait();
        }
        queue.Dispose();
    }

    public ILogger CreateLogger(string categoryName) {
        var key = levels.Keys
            .FirstOrDefault(x => categoryName.StartsWith(x));
        
        if(key == null) {
            return new MyLogger(this, categoryName, defaultLevel);
        }
        return new MyLogger(this, categoryName, levels[key]);
    }

    /// <summary>
    /// 書き込み登録
    /// </summary>
    public void Offer(string text) {
        queue.TryAdd(text);
    }

    /// <summary>
    /// 書き込みスレッド
    /// </summary>
    private void WritingAction() {
        StreamWriter writer = null;
        string path = "";

        Action close = () => {
            if (writer != null) {
                writer.Close();
                writer = null;
            }
        };
        Action open = () => {
            var tmp = $"{root}/{DateTime.Now:yyyyMMdd}.log";
            if(writer == null || path != tmp) {
                close();
                writer = new StreamWriter(tmp, true, Encoding.UTF8);
                path = tmp;
            }
        };

        while (!queue.IsCompleted) {
            while (queue.TryTake(out var item, 3000)) {
                try {
                    open();
                    writer.WriteLine(item);
                    writer.Flush();
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    close();
                }
            }
            close();
        }
    }
}

/// <summary>
/// ロガー
/// </summary>
class MyLogger : ILogger
{
    private string categoryName;
    private MyLoggerProvider provider;
    private LogLevel level;

    private static Dictionary<LogLevel, string> shortNames = new Dictionary<LogLevel, string>() {
            { LogLevel.Trace, "Trac" },
            { LogLevel.Debug, "Debu" },
            { LogLevel.Information, "Info" },
            { LogLevel.Warning, "!Warn" },
            { LogLevel.Error, "!Erro" },
            { LogLevel.Critical, "!Crit" },
            { LogLevel.None, "!None" },
        };

    public MyLogger(MyLoggerProvider provider, string categoryName, LogLevel level) {
        this.provider = provider;
        this.level = level;
        this.categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) {
        return null;
    }
    public bool IsEnabled(LogLevel logLevel) {
        return logLevel >= level;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception
        , Func<TState, Exception, string> formatter) {

        if (!IsEnabled(logLevel)) return;

        var msg = $"{DateTime.Now:HH:mm:ss.fff} [{shortNames[logLevel]}|{categoryName}] {formatter(state, exception)}";
        if (exception != null) {
            msg += Environment.NewLine;
            msg += exception.ToString();
        }
        provider.Offer(msg);
    }
}
