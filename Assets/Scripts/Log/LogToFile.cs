using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 异步线程专用的 Logger 类，在线程结束时一次性输出所有日志
/// </summary>
public class AsyncLogger : IDisposable
{
    private readonly List<string> _logEntries = new List<string>();
    private readonly string _caller;
    private readonly bool _output;
    private bool _disposed = false;

    /// <summary>
    /// 创建一个异步线程专用的 Logger 实例
    /// </summary>
    /// <param name="caller">日志上下文（通常是类名）</param>
    /// <param name="output">是否输出日志</param>
    public AsyncLogger(string caller = null, bool output = true)
    {
        _caller = caller;
        _output = output;
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public void Info(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Info, message, subModule);
    }

    /// <summary>
    /// 记录普通日志
    /// </summary>
    public void Log(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Log, message, subModule);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public void Warning(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Warning, message, subModule);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public void Error(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Error, message, subModule);
    }

    /// <summary>
    /// 记录严重错误日志
    /// </summary>
    public void Critical(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Critical, message, subModule);
    }

    /// <summary>
    /// 记录异常
    /// </summary>
    public void Exception(Exception ex, string subModule = null)
    {
        string message = $"{ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        AddLog(LogManager.LogLevel.Critical, message, subModule);
    }

    /// <summary>
    /// 往日志列表中添加一条日志
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    /// <param name="subModule">（可选）模块的子模块名</param>
    private void AddLog(LogManager.LogLevel level, string message, string subModule = null)
    {
        if (!_output)
            return;

        string timestamp = DateTime.Now.ToString("[yyyy-MM-dd] HH:mm:ss.ffffff");
        string levelStr = level.ToString().ToUpper();

        // 如果有子模块名就用 "." 符号串接到caller后面
        string callerStr =
            string.IsNullOrEmpty(_caller) ? ""
            : string.IsNullOrEmpty(subModule) ? $"[{_caller}] "
            : $"[{_caller}.{subModule}] ";

        string logEntry = $"{timestamp} [{levelStr}] {callerStr}{message}";

        _logEntries.Add(logEntry);
    }

    /// <summary>
    /// 一次性输出所有累积的日志
    /// </summary>
    public void Dump()
    {
        if (!_output || _logEntries.Count == 0)
            return;

        // 构建完整的日志输出
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"====== 异步线程日志输出 [{_caller}] ======\n");

        foreach (var logEntry in _logEntries)
        {
            sb.AppendLine(logEntry);
        }

        // 输出到主日志系统
        string fullLog = sb.ToString();
        LogManager.Info(fullLog, _caller, _output);

        // 奇异诡谲 但是能用
        LogManager.Info($"====== 异步线程日志结束 ======\n", _caller, _output);
    }

    /// <summary>
    /// 清空所有累积的日志
    /// </summary>
    public void Clear()
    {
        _logEntries.Clear();
    }

    /// <summary>
    /// 获取累积的日志数量
    /// </summary>
    public int GetLogCount()
    {
        return _logEntries.Count;
    }

    /// <summary>
    /// 实现 IDisposable 接口，在销毁时自动输出日志
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Dump();
            _disposed = true;
        }
    }

    /// <summary>
    /// 析构函数，确保日志被输出
    /// </summary>
    ~AsyncLogger()
    {
        if (!_disposed)
        {
            // 注意：在析构函数中不能调用LogManager，因为可能已经在主线程之外
            // 这里只是确保资源被清理，实际日志输出应该在Dispose方法中完成
        }
    }
}

public static class LogManager
{
    private static string logFilePath;
    private static StreamWriter logWriter;
    private static bool isInitialized = false;
    private static bool isShuttingDown = false;

    // 日志级别
    public enum LogLevel
    {
        Log,
        Info,
        Warning,
        Error,
        Critical,
    }

    // 当前日志级别
    public static LogLevel CurrentLogLevel = LogLevel.Log;

    // 初始化日志系统
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized || isShuttingDown)
            return;

        // 创建日志目录
        string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // 创建日志文件（按日期命名）
        string dateString = DateTime.Now.ToString("yyyy-MM-dd");
        logFilePath = Path.Combine(logDirectory, $"game_log_{dateString}.txt");

        try
        {
            // 使用 FileShare.ReadWrite 允许其他进程读取文件
            var fileStream = new FileStream(
                logFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite
            );

            // 创建或追加到日志文件b
            logWriter = new StreamWriter(fileStream, Encoding.UTF8);
            logWriter.AutoFlush = true;

            // 写入日志文件头
            logWriter.WriteLine("==========================================");
            logWriter.WriteLine($"Game Log - {DateTime.Now}");
            logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
            logWriter.WriteLine($"Platform: {Application.platform}");
            logWriter.WriteLine($"Product Name: {Application.productName}");
            logWriter.WriteLine("==========================================\n");

            isInitialized = true;

#if UNITY_EDITOR
            // 注册编辑器播放模式状态变化事件
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
            // 注册应用退出事件
            Application.quitting += OnApplicationQuit;

            Info($"======Log.开启日志系统======\n", nameof(LogManager));
        }
        catch (Exception ex)
        {
            Debug.LogError($"初始化日志系统失败: {ex.Message}");
        }
    }

    // 编辑器专用的播放模式状态变化处理
#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Shutdown();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            // 重置状态，以便下次进入播放模式时可以重新初始化
            isShuttingDown = false;
            isInitialized = false;
        }
    }
#endif

    // 应用退出时清理资源
    private static void OnApplicationQuit()
    {
        Shutdown();
    }

    private static void Shutdown()
    {
        if (isShuttingDown)
            return;
        isShuttingDown = true;

        if (logWriter != null && isInitialized)
        {
            try
            {
                logWriter.WriteLine();

                // 丑 但是比较有用
                Info("======Log.关闭日志系统======\n", nameof(LogManager));

                logWriter.WriteLine("==========================================");
                logWriter.WriteLine($"应用程序退出 - {DateTime.Now}");
                logWriter.WriteLine("==========================================\n\n\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入关闭日志失败: {ex.Message}");
            }
        }

        // 关闭日志写入器
        if (logWriter != null)
        {
            try
            {
                logWriter.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"关闭日志写入器失败: {ex.Message}");
            }
            finally
            {
                logWriter = null;
            }
        }

        // 在编辑器中取消注册事件
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

        Application.quitting -= OnApplicationQuit;
        isInitialized = false;
        isShuttingDown = false;
    }

    // 写入日志
    private static void Log(LogLevel level, string message, string caller = null)
    {
        // 检查日志级别
        if (!Enum.IsDefined(typeof(LogLevel), level))
            return;

        // 确保已初始化
        if (!isInitialized)
            Initialize();

        if (logWriter == null)
            return;

        try
        {
            // 构建日志条目
            string timestamp = DateTime.Now.ToString("[yyyy-MM-dd] HH:mm:ss.ffffff");
            string levelStr = level.ToString().ToUpper();
            string contextStr = string.IsNullOrEmpty(caller) ? "" : $"[{caller}] ";

            string logEntry = $"{timestamp} [{levelStr}] {contextStr}{message}";

            // 写入文件和控制台
            logWriter.WriteLine(logEntry);

            // 同时在 Unity 控制台输出（根据级别使用不同的方法）
            switch (level)
            {
                case LogLevel.Log:
                case LogLevel.Info:
                    Debug.Log(logEntry); // Log 和 Info 使用相同的输出，其余同理
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(logEntry);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(logEntry);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"写入日志失败: {ex.Message}");
        }
    }

    // 在不想输出的时候 可以关闭输出Log
    public static void Log(string message, string caller = null, bool output = true)
    {
        if (output)
            Log(LogLevel.Log, message, caller);
    }

    public static void Info(string message, string caller = null, bool output = true)
    {
        if (output)
            Log(LogLevel.Info, message, caller);
    }

    public static void Warning(string message, string caller = null, bool output = true)
    {
        if (output)
            Log(LogLevel.Warning, message, caller);
    }

    public static void Error(string message, string caller = null, bool output = true)
    {
        if (output)
            Log(LogLevel.Error, message, caller);
    }

    // 记录异常
    public static void Exception(Exception ex, string context = null, bool output = true)
    {
        if (output)
        {
            string message = $"{ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Critical(message, context);
        }
    }

    private static void Critical(string message, string context = null, bool output = true)
    {
        if (output)
            Log(LogLevel.Critical, message, context);
    }

    // 获取日志文件路径
    public static string GetLogFilePath()
    {
        return logFilePath;
    }

    // 打开日志目录
    public static void OpenLogDirectory()
    {
        string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
        if (Directory.Exists(logDirectory))
        {
            Application.OpenURL($"file://{logDirectory}");
        }
        else
        {
            Debug.LogWarning("日志目录不存在");
        }
    }
}
