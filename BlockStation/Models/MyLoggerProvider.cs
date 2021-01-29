using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// ファイル出力ロガープロバイダー
/// </summary>
public class MyLoggerProvider : ILoggerProvider
{
    private Task task = null;
    private BlockingCollection<String> queue = new BlockingCollection<string>();
    
    /// <summary>
    /// ログファイルパス
    /// </summary>
    private string root;

    /// <summary>
    /// 出力レベル指定テーブル
    /// </summary>
    private Dictionary<string, LogLevel> levels = new Dictionary<string, LogLevel>();

    /// <summary>
    /// デフォルト出力レベル
    /// </summary>
    private LogLevel defaultLevel = LogLevel.Information;

    /// <summary>
    /// ログ削除期限（日）
    /// </summary>
    private int deleteSpan = 30;

    /// <summary>
    /// ログ削除実行日時
    /// </summary>
    private DateTime prevDelete = DateTime.Now.AddDays(-3);

    //----------

    public MyLoggerProvider(IConfigurationSection configration, string contentPath) {
        root = configration.GetValue<string>("RootDirectory");
        if (root.StartsWith(".")) {
            root = contentPath + "/" + root;
        }

        foreach (var x in configration.GetSection("LogLevel").GetChildren()) {
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
            int count = 0;
            while (count < 1000 && queue.TryTake(out var item, 3000)) {
                try {
                    open();
                    writer.WriteLine(item);
                    writer.Flush();
                    count++;
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    close();
                }
            }
            close();
            DeleteLog();
        }
    }

    /// <summary>
    /// ログ削除
    /// </summary>
    private void DeleteLog() {
        DateTime now = DateTime.Now;
        if(prevDelete.Day == now.Day) return;

        var border = now.AddDays(-deleteSpan);
        var files = Directory.GetFiles(root,"*.log");

        foreach(var f in files) {
            if (f.CompareTo($"{border:yyyyMMdd}.log") < 0) {
                try {
                    File.Delete(f);
                }catch(Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        prevDelete = now;
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
