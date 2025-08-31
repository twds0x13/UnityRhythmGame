using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class LogFile
{
    private static string logFilePath;
    private static StreamWriter logWriter;
    private static bool initialized = false;

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
        if (initialized)
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
            // 创建或追加到日志文件
            logWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
            logWriter.AutoFlush = true;

            // 写入日志文件头
            logWriter.WriteLine("==========================================");
            logWriter.WriteLine($"Game Log - {DateTime.Now}");
            logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
            logWriter.WriteLine($"Platform: {Application.platform}");
            logWriter.WriteLine($"Product Name: {Application.productName}");
            logWriter.WriteLine("==========================================");
            logWriter.WriteLine();

            initialized = true;

            // 注册应用退出事件
            Application.quitting += OnApplicationQuit;

            Debug.Log($"日志系统已初始化，日志文件: {logFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"初始化日志系统失败: {ex.Message}");
        }
    }

    // 应用退出时清理资源
    private static void OnApplicationQuit()
    {
        if (logWriter != null)
        {
            try
            {
                logWriter.WriteLine();
                logWriter.WriteLine($"应用程序退出 - {DateTime.Now}");
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
        initialized = false;
    }

    // 写入日志
    public static void Log(LogLevel level, string message, string context = null)
    {
        // 检查日志级别
        if ((int)level < (int)CurrentLogLevel)
            return;

        // 确保已初始化
        if (!initialized)
            Initialize();

        if (logWriter == null)
            return;

        try
        {
            // 构建日志条目
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string contextStr = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";

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

    // 便捷方法
    public static void Log(string message, string context = null)
    {
        Log(LogLevel.Log, message, context);
    }

    public static void Info(string message, string context = null)
    {
        Log(LogLevel.Info, message, context);
    }

    public static void Warning(string message, string context = null)
    {
        Log(LogLevel.Warning, message, context);
    }

    public static void Error(string message, string context = null)
    {
        Log(LogLevel.Error, message, context);
    }

    public static void Critical(string message, string context = null)
    {
        Log(LogLevel.Critical, message, context);
    }

    // 记录异常
    public static void Exception(Exception ex, string context = null)
    {
        string message = $"{ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        Critical(message, context);
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
