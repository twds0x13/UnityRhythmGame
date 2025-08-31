using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class LogFile
{
    private static string logFilePath;
    private static StreamWriter logWriter;
    private static bool initialized = false;

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
        if (initialized)
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
            // ������׷�ӵ���־�ļ�
            logWriter = new StreamWriter(logFilePath, true, Encoding.UTF8);
            logWriter.AutoFlush = true;

            // д����־�ļ�ͷ
            logWriter.WriteLine("==========================================");
            logWriter.WriteLine($"Game Log - {DateTime.Now}");
            logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
            logWriter.WriteLine($"Platform: {Application.platform}");
            logWriter.WriteLine($"Product Name: {Application.productName}");
            logWriter.WriteLine("==========================================");
            logWriter.WriteLine();

            initialized = true;

            // ע��Ӧ���˳��¼�
            Application.quitting += OnApplicationQuit;

            Debug.Log($"��־ϵͳ�ѳ�ʼ������־�ļ�: {logFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"��ʼ����־ϵͳʧ��: {ex.Message}");
        }
    }

    // Ӧ���˳�ʱ������Դ
    private static void OnApplicationQuit()
    {
        if (logWriter != null)
        {
            try
            {
                logWriter.WriteLine();
                logWriter.WriteLine($"Ӧ�ó����˳� - {DateTime.Now}");
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
        initialized = false;
    }

    // д����־
    public static void Log(LogLevel level, string message, string context = null)
    {
        // �����־����
        if ((int)level < (int)CurrentLogLevel)
            return;

        // ȷ���ѳ�ʼ��
        if (!initialized)
            Initialize();

        if (logWriter == null)
            return;

        try
        {
            // ������־��Ŀ
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string contextStr = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";

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

    // ��ݷ���
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

    // ��¼�쳣
    public static void Exception(Exception ex, string context = null)
    {
        string message = $"{ex.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}";
        Critical(message, context);
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
