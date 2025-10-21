using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// �첽�߳�ר�õ� Logger �࣬���߳̽���ʱһ�������������־
/// </summary>
public class AsyncLogger : IDisposable
{
    private readonly List<string> _logEntries = new List<string>();
    private readonly string _caller;
    private readonly bool _output;
    private bool _disposed = false;

    /// <summary>
    /// ����һ���첽�߳�ר�õ� Logger ʵ��
    /// </summary>
    /// <param name="caller">��־�����ģ�ͨ����������</param>
    /// <param name="output">�Ƿ������־</param>
    public AsyncLogger(string caller = null, bool output = true)
    {
        _caller = caller;
        _output = output;
    }

    /// <summary>
    /// ��¼��Ϣ��־
    /// </summary>
    public void Info(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Info, message, subModule);
    }

    /// <summary>
    /// ��¼��ͨ��־
    /// </summary>
    public void Log(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Log, message, subModule);
    }

    /// <summary>
    /// ��¼������־
    /// </summary>
    public void Warning(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Warning, message, subModule);
    }

    /// <summary>
    /// ��¼������־
    /// </summary>
    public void Error(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Error, message, subModule);
    }

    /// <summary>
    /// ��¼���ش�����־
    /// </summary>
    public void Critical(string message, string subModule = null)
    {
        AddLog(LogManager.LogLevel.Critical, message, subModule);
    }

    /// <summary>
    /// ��¼�쳣
    /// </summary>
    public void Exception(Exception ex, string subModule = null)
    {
        string message = $"{ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        AddLog(LogManager.LogLevel.Critical, message, subModule);
    }

    /// <summary>
    /// ����־�б������һ����־
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    /// <param name="subModule">����ѡ��ģ�����ģ����</param>
    private void AddLog(LogManager.LogLevel level, string message, string subModule = null)
    {
        if (!_output)
            return;

        string timestamp = DateTime.Now.ToString("[yyyy-MM-dd] HH:mm:ss.ffffff");
        string levelStr = level.ToString().ToUpper();

        // �������ģ�������� "." ���Ŵ��ӵ�caller����
        string callerStr =
            string.IsNullOrEmpty(_caller) ? ""
            : string.IsNullOrEmpty(subModule) ? $"[{_caller}] "
            : $"[{_caller}.{subModule}] ";

        string logEntry = $"{timestamp} [{levelStr}] {callerStr}{message}";

        _logEntries.Add(logEntry);
    }

    /// <summary>
    /// һ������������ۻ�����־
    /// </summary>
    public void Dump()
    {
        if (!_output || _logEntries.Count == 0)
            return;

        // ������������־���
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"====== �첽�߳���־��� [{_caller}] ======\n");

        foreach (var logEntry in _logEntries)
        {
            sb.AppendLine(logEntry);
        }

        // ���������־ϵͳ
        string fullLog = sb.ToString();
        LogManager.Info(fullLog, _caller, _output);

        // ������� ��������
        LogManager.Info($"====== �첽�߳���־���� ======\n", _caller, _output);
    }

    /// <summary>
    /// ��������ۻ�����־
    /// </summary>
    public void Clear()
    {
        _logEntries.Clear();
    }

    /// <summary>
    /// ��ȡ�ۻ�����־����
    /// </summary>
    public int GetLogCount()
    {
        return _logEntries.Count;
    }

    /// <summary>
    /// ʵ�� IDisposable �ӿڣ�������ʱ�Զ������־
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
    /// ����������ȷ����־�����
    /// </summary>
    ~AsyncLogger()
    {
        if (!_disposed)
        {
            // ע�⣺�����������в��ܵ���LogManager����Ϊ�����Ѿ������߳�֮��
            // ����ֻ��ȷ����Դ������ʵ����־���Ӧ����Dispose���������
        }
    }
}

public static class LogManager
{
    private static string logFilePath;
    private static StreamWriter logWriter;
    private static bool isInitialized = false;
    private static bool isShuttingDown = false;

    // ��־����
    public enum LogLevel
    {
        Log,
        Info,
        Warning,
        Error,
        Critical,
    }

    // ��ǰ��־����
    public static LogLevel CurrentLogLevel = LogLevel.Log;

    // ��ʼ����־ϵͳ
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized || isShuttingDown)
            return;

        // ������־Ŀ¼
        string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // ������־�ļ���������������
        string dateString = DateTime.Now.ToString("yyyy-MM-dd");
        logFilePath = Path.Combine(logDirectory, $"game_log_{dateString}.txt");

        try
        {
            // ʹ�� FileShare.ReadWrite �����������̶�ȡ�ļ�
            var fileStream = new FileStream(
                logFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite
            );

            // ������׷�ӵ���־�ļ�b
            logWriter = new StreamWriter(fileStream, Encoding.UTF8);
            logWriter.AutoFlush = true;

            // д����־�ļ�ͷ
            logWriter.WriteLine("==========================================");
            logWriter.WriteLine($"Game Log - {DateTime.Now}");
            logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
            logWriter.WriteLine($"Platform: {Application.platform}");
            logWriter.WriteLine($"Product Name: {Application.productName}");
            logWriter.WriteLine("==========================================\n");

            isInitialized = true;

#if UNITY_EDITOR
            // ע��༭������ģʽ״̬�仯�¼�
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
            // ע��Ӧ���˳��¼�
            Application.quitting += OnApplicationQuit;

            Info($"======Log.������־ϵͳ======\n", nameof(LogManager));
        }
        catch (Exception ex)
        {
            Debug.LogError($"��ʼ����־ϵͳʧ��: {ex.Message}");
        }
    }

    // �༭��ר�õĲ���ģʽ״̬�仯����
#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Shutdown();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            // ����״̬���Ա��´ν��벥��ģʽʱ�������³�ʼ��
            isShuttingDown = false;
            isInitialized = false;
        }
    }
#endif

    // Ӧ���˳�ʱ������Դ
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

                // �� ���ǱȽ�����
                Info("======Log.�ر���־ϵͳ======\n", nameof(LogManager));

                logWriter.WriteLine("==========================================");
                logWriter.WriteLine($"Ӧ�ó����˳� - {DateTime.Now}");
                logWriter.WriteLine("==========================================\n\n\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"д��ر���־ʧ��: {ex.Message}");
            }
        }

        // �ر���־д����
        if (logWriter != null)
        {
            try
            {
                logWriter.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"�ر���־д����ʧ��: {ex.Message}");
            }
            finally
            {
                logWriter = null;
            }
        }

        // �ڱ༭����ȡ��ע���¼�
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

        Application.quitting -= OnApplicationQuit;
        isInitialized = false;
        isShuttingDown = false;
    }

    // д����־
    private static void Log(LogLevel level, string message, string caller = null)
    {
        // �����־����
        if (!Enum.IsDefined(typeof(LogLevel), level))
            return;

        // ȷ���ѳ�ʼ��
        if (!isInitialized)
            Initialize();

        if (logWriter == null)
            return;

        try
        {
            // ������־��Ŀ
            string timestamp = DateTime.Now.ToString("[yyyy-MM-dd] HH:mm:ss.ffffff");
            string levelStr = level.ToString().ToUpper();
            string contextStr = string.IsNullOrEmpty(caller) ? "" : $"[{caller}] ";

            string logEntry = $"{timestamp} [{levelStr}] {contextStr}{message}";

            // д���ļ��Ϳ���̨
            logWriter.WriteLine(logEntry);

            // ͬʱ�� Unity ����̨��������ݼ���ʹ�ò�ͬ�ķ�����
            switch (level)
            {
                case LogLevel.Log:
                case LogLevel.Info:
                    Debug.Log(logEntry); // Log �� Info ʹ����ͬ�����������ͬ��
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
            Debug.LogError($"д����־ʧ��: {ex.Message}");
        }
    }

    // �ڲ��������ʱ�� ���Թر����Log
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

    // ��¼�쳣
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

    // ��ȡ��־�ļ�·��
    public static string GetLogFilePath()
    {
        return logFilePath;
    }

    // ����־Ŀ¼
    public static void OpenLogDirectory()
    {
        string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
        if (Directory.Exists(logDirectory))
        {
            Application.OpenURL($"file://{logDirectory}");
        }
        else
        {
            Debug.LogWarning("��־Ŀ¼������");
        }
    }
}
