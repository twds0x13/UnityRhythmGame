using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using UnityEngine;

namespace JsonLoader
{
    /// <summary>
    /// �����ļ��ĳ�ʼ���ͼ��
    /// </summary>
    public static class FileInitializationManager
    {
        private const string INITIALIZATION_FLAG = "FilesInitialized";
        private static readonly string[] REQUIRED_FILES = new string[] { "story.zip" };

        /// <summary>
        /// ��鲢ִ���ļ���ʼ��
        /// </summary>
        public static async UniTask<bool> InitializeFilesAsync(
            bool forceReinitialize = false,
            CancellationToken cancellationToken = default
        )
        {
            using (var logger = new AsyncLogger(nameof(FileInitializationManager), true))
            {
                try
                {
                    // ����Ƿ��Ѿ���ʼ��
                    if (!forceReinitialize && IsInitialized())
                    {
                        logger.Info("�ļ��Ѿ���ʼ����������ʼ������");
                        return true;
                    }

                    logger.Info("��ʼ�ļ���ʼ������...");

                    // ��鲢����Ŀ¼
                    EnsureDirectoryExists();

                    // ����Ҫ�ļ��Ƿ���ڣ�������������Դ�StreamingAssets����
                    bool success = await CheckAndCopyRequiredFilesAsync(logger, cancellationToken);

                    if (success)
                    {
                        // ��ǳ�ʼ�����
                        SetInitialized();
                        logger.Info("�ļ���ʼ�����");
                    }
                    else
                    {
                        logger.Error("�ļ���ʼ��ʧ�� - ��Ҫ�ļ����������޷���StreamingAssets����");
                    }

                    return success;
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("�ļ���ʼ����ȡ��");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"�ļ���ʼ���쳣: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// ����Ҫ�ļ��Ƿ���ڣ�������������StreamingAssets����
        /// </summary>
        private static async UniTask<bool> CheckAndCopyRequiredFilesAsync(
            AsyncLogger logger,
            CancellationToken cancellationToken
        )
        {
            foreach (string fileName in REQUIRED_FILES)
            {
                string filePath = Path.Combine(BaseJsonLoader.ZipPath, fileName);
                if (!File.Exists(filePath))
                {
                    logger.Warning($"��Ҫ�ļ�������: {filePath}�����Դ�StreamingAssets����");

                    bool copySuccess = await BaseJsonLoader.TryCopyFromStreamingAssetsAsync(
                        fileName,
                        fileName,
                        logger,
                        cancellationToken
                    );
                    if (!copySuccess)
                    {
                        logger.Error($"��StreamingAssets�����ļ�ʧ��: {fileName}");
                        return false;
                    }

                    logger.Info($"�ɹ���StreamingAssets�����ļ�: {fileName}");
                }
                else
                {
                    logger.Info($"��Ҫ�ļ�����: {filePath}");
                }
            }
            return true;
        }

        /// <summary>
        /// ����ļ��Ƿ��Ѿ���ʼ��
        /// </summary>
        public static bool IsInitialized()
        {
            return PlayerPrefs.GetInt(INITIALIZATION_FLAG, 0) == 1;
        }

        /// <summary>
        /// ����ļ�Ϊ�ѳ�ʼ��
        /// </summary>
        public static void SetInitialized()
        {
            PlayerPrefs.SetInt(INITIALIZATION_FLAG, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// ���ó�ʼ��״̬�����ڲ��Ի����³�ʼ����
        /// </summary>
        public static void ResetInitialization()
        {
            PlayerPrefs.DeleteKey(INITIALIZATION_FLAG);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// ȷ��Ŀ¼����
        /// </summary>
        private static void EnsureDirectoryExists()
        {
            using (var logger = new AsyncLogger(nameof(FileInitializationManager), true))
            {
                if (!Directory.Exists(BaseJsonLoader.ZipPath))
                {
                    Directory.CreateDirectory(BaseJsonLoader.ZipPath);
                    logger.Info($"����Ŀ¼: {BaseJsonLoader.ZipPath}");
                }
                else
                {
                    logger.Info($"Ŀ¼�Ѵ���: {BaseJsonLoader.ZipPath}");
                }
            }
        }

        /// <summary>
        /// ��ȡ�ļ���Ϣͳ��
        /// </summary>
        public static FileInfoStats GetFileInfoStats()
        {
            var stats = new FileInfoStats();
            string[] files = Directory.GetFiles(BaseJsonLoader.ZipPath, "*.zip");

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                stats.TotalFiles++;
                stats.TotalSize += fileInfo.Length;
                stats.FileDetails.Add(
                    new FileDetail
                    {
                        FileName = Path.GetFileName(file),
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime,
                    }
                );
            }

            return stats;
        }
    }

    /// <summary>
    /// �ļ���Ϣͳ��
    /// </summary>
    public class FileInfoStats
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public List<FileDetail> FileDetails { get; set; } = new List<FileDetail>();
    }

    /// <summary>
    /// �ļ�����
    /// </summary>
    public class FileDetail
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// �����ѹ <see cref="ZipFile"/> �����н��� <see cref="Newtonsoft.Json"/> �ļ���
    /// ������Ϊ���������࣬��������ƻ�������ȡ������
    /// </summary>
    public class BaseJsonLoader
    {
        public const string DEFAULT_JSON_FILE_NAME = "Default.json";

        public static readonly string ZipPath = Application.persistentDataPath;
        public static readonly string StreamingAssetsPath = Application.streamingAssetsPath;

        // �ǳ���Ҫ��������ֶ����ã����ڷ����л�ʱ��ʧ����������Ϣ
        public static JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        #region Save To File
        public static bool TrySaveToZip(string zipFileName, string content, bool output = true)
        {
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                byte[] buffer = Encoding.Unicode.GetBytes(content);
                string fullPath = Path.Combine(ZipPath, zipFileName);

                try
                {
                    // ȷ��Ŀ¼����
                    string directory = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        logger.Info($"����Ŀ¼: {directory}");
                    }

                    // ʹ�� FileShare.ReadWrite �����������̷���
                    using (
                        var fileStream = new FileStream(
                            fullPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.ReadWrite
                        )
                    )
                    using (var zipStream = new ZipOutputStream(fileStream))
                    {
                        zipStream.SetLevel(9);

                        var entry = new ZipEntry(DEFAULT_JSON_FILE_NAME)
                        {
                            DateTime = DateTime.Now,
                            Comment = "Auto Created by JsonManager",
                            IsUnicodeText = true,
                        };

                        zipStream.PutNextEntry(entry);

                        using (var memoryStream = new MemoryStream(buffer))
                        {
                            StreamUtils.Copy(memoryStream, zipStream, new byte[2048]);
                        }

                        zipStream.CloseEntry();
                    }

                    logger.Info($"�ɹ����� Zip �ļ�: {fullPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"���� Zip �ļ�ʧ��: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// ���� <see cref=" JsonConvert"/> �⽫��������л� <paramref name="serializeObject"/> ����ѹ�� .json �ļ��洢�� <see cref="Application.persistentDataPath"/>���ļ���Ϊ <paramref name="zipFileName"/>
        /// </summary>
        public static bool TrySaveJsonToZip<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings
        )
        {
            return TrySaveJsonToZip(zipFileName, serializeObject, settings, DEFAULT_JSON_FILE_NAME);
        }

        /// <summary>
        /// ��ʵ��� <paramref name="JsonName"/> ֻ��Ϊ���û������������Ϊ <see cref="LoadJsonFromZip{T}(string, ref T)"/> ��Ҫ���� <paramref name="JsonName"/>
        /// </summary>
        public static bool TrySaveJsonToZip<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings,
            string JsonName
        )
        {
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), true))
            {
                try
                {
                    string Json = JsonConvert.SerializeObject(serializeObject, settings);
                    logger.Info($"���������л���JSON ����: {Json.Length} �ַ�");
                    return TrySaveToZip(zipFileName, Json);
                }
                catch (JsonReaderException ex)
                {
                    logger.Exception(ex);
                    logger.Warning(
                        $"����ʱ��⵽ {zipFileName} �ļ��е� {JsonName} �﷨�ṹ����"
                    );
                    return false;
                }
                catch (JsonSerializationException ex)
                {
                    logger.Exception(ex);
                    logger.Warning(
                        $"����ʱ��⵽ {zipFileName} �ļ��е� {JsonName} ��������ת������"
                    );
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Warning($"����ʱ��⵽ {zipFileName} �ļ��е� {JsonName} ��������");
                    return false;
                }
            }
        }

        /// <summary>
        /// �첽������� ZIP �ļ�
        /// </summary>
        public static async UniTask<bool> TrySaveJsonToZipAsync<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings,
            string jsonName = DEFAULT_JSON_FILE_NAME,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // �����첽��־��¼��
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                logger.Info($"��ʼ�첽�����ļ� [{zipFileName}]");

                try
                {
                    // ���̳߳���ִ�����л��ͱ������
                    return await UniTask.RunOnThreadPool(
                        () =>
                        {
                            try
                            {
                                // ���ȡ������
                                cancellationToken.ThrowIfCancellationRequested();

                                // ���л�����
                                string json = JsonConvert.SerializeObject(
                                    serializeObject,
                                    settings
                                );
                                logger.Info($"���������л���JSON ����: {json.Length} �ַ�");

                                // ���浽 ZIP �ļ�
                                bool saveSuccess = TrySaveToZip(zipFileName, json, false);

                                if (saveSuccess)
                                {
                                    logger.Info($"�ɹ��첽�����ļ�: {zipFileName}");
                                }
                                else
                                {
                                    logger.Error($"�첽�����ļ�ʧ��: {zipFileName}");
                                }

                                return saveSuccess;
                            }
                            catch (OperationCanceledException)
                            {
                                logger.Warning("���������ȡ��");
                                return false;
                            }
                            catch (JsonReaderException ex)
                            {
                                logger.Exception(ex);
                                logger.Error($"JSON �﷨����: {ex.Message}");
                                return false;
                            }
                            catch (JsonSerializationException ex)
                            {
                                logger.Exception(ex);
                                logger.Error($"JSON ���л�����: {ex.Message}");
                                return false;
                            }
                            catch (Exception ex)
                            {
                                logger.Exception(ex);
                                logger.Error($"����ʧ��: {ex.Message}");
                                return false;
                            }
                        },
                        cancellationToken: cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("��������ȡ��");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"�첽����ʧ��: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// �첽������� ZIP �ļ����򻯰棩
        /// </summary>
        public static async UniTask<bool> TrySaveJsonToZipAsync<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            return await TrySaveJsonToZipAsync(
                zipFileName,
                serializeObject,
                settings,
                DEFAULT_JSON_FILE_NAME,
                output,
                cancellationToken
            );
        }

        #endregion

        #region Load From File

        /// <summary>
        /// ���� <see cref=" JsonConvert"/> ���λ�� <see cref="ZipPath"/> Ŀ¼����Ϊ <paramref name="ZipFileName"/> �� <see cref="ZipFile"/> �ж�ȡ��������л�Ϊ <paramref name="RefObject"/> ��json�ļ�
        /// </summary>
        public static bool TryLoadJsonFromZip<T>(
            string ZipFileName,
            out T RefObject,
            T Default = default,
            bool output = true
        )
        {
            return TryLoadJsonFromZip(
                ZipFileName,
                out RefObject,
                DEFAULT_JSON_FILE_NAME,
                Default,
                output
            );
        }

        /// <summary>
        /// �첽�� ZIP �ļ����� JSON �������л�
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadJsonFromZipAsync<T>(
            string zipFileName,
            string jsonName = DEFAULT_JSON_FILE_NAME,
            T defaultValue = default,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                logger.Info($"��ʼ�첽���� ZIP �ļ� [{zipFileName}] �е� [{jsonName}]");

                try
                {
                    // ���̳߳���ִ��ͬ�����ز���
                    return await UniTask.RunOnThreadPool(
                        () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            bool success = TryLoadJsonFromZip(
                                zipFileName,
                                out T result,
                                jsonName,
                                defaultValue,
                                false // �ں�̨�߳��в������־
                            );

                            if (success)
                            {
                                logger.Info(
                                    $"�ɹ��첽���� ZIP �ļ� [{zipFileName}] �е� [{jsonName}]"
                                );
                            }
                            else
                            {
                                logger.Error($"�첽���� ZIP �ļ�ʧ��: {zipFileName}");
                            }

                            return (success, result);
                        },
                        cancellationToken: cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    logger.Warning($"ZIP �ļ����ر�ȡ��: {zipFileName}");
                    return (false, defaultValue);
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"�첽���� ZIP �ļ��쳣: {ex.Message}");
                    return (false, defaultValue);
                }
            }
        }

        /// <summary>
        /// �첽�� ZIP �ļ����� JSON �������л����򻯰棩
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadJsonFromZipAsync<T>(
            string zipFileName,
            T defaultValue = default,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            return await TryLoadJsonFromZipAsync(
                zipFileName,
                DEFAULT_JSON_FILE_NAME,
                defaultValue,
                output,
                cancellationToken
            );
        }

        /// <summary>
        /// �첽�� ZIP �ļ����� JSON �������л���ʹ��Ĭ��ֵ��
        /// </summary>
        public static async UniTask<T> LoadJsonFromZipAsync<T>(
            string zipFileName,
            string jsonName = DEFAULT_JSON_FILE_NAME,
            T defaultValue = default,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            var (success, result) = await TryLoadJsonFromZipAsync<T>(
                zipFileName,
                jsonName,
                defaultValue,
                output,
                cancellationToken
            );
            return result;
        }

        /// <summary>
        /// �첽���� JSON �ļ��������л���ʹ�� <see cref="AsyncLogger"/> ��¼��־
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadObjectFromJsonAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // �����첽��־��¼��
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                logger.Info($"��ʼ�첽�����ļ� [{fileName}]");

                T result = default;

                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"�ļ�������: {fullPath}");
                    return (false, result);
                }

                try
                {
                    // ʹ�� UniTask.RunOnThreadPool �ں�̨�߳�ִ��ͬ������
                    var loadResult = await UniTask.RunOnThreadPool(
                        () => TryLoadObjectFromJson(fileName, out result, false),
                        cancellationToken: cancellationToken
                    );

                    // ��¼���
                    if (loadResult)
                    {
                        logger.Info($"�ɹ��첽���� {typeof(T).Name} ʵ��");
                    }
                    else
                    {
                        logger.Error("�첽����ʧ��");
                    }

                    return (loadResult, result);
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("JSON ���ر�ȡ��");
                    return (false, result);
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    return (false, result);
                }
            }
        }

        /// <summary>
        /// ���� <see cref=" JsonConvert"/> ���λ�� <see cref="ZipPath"/> Ŀ¼�µ� <paramref name="fileName"/> �ļ������л�������
        /// </summary>
        public static bool TryLoadObjectFromJson<T>(
            string fileName,
            out T result,
            bool output = true
        )
        {
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                logger.Info($"======Json.��ʼ�����ļ� [{fileName}] ======");

                result = default;
                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"�ļ�������: {fullPath}");
                    return false;
                }

                try
                {
                    // ��ȡ ZIP �ļ�����Ϊ�ֽ�����
                    if (!TryReadZipContentBytes(fullPath, out byte[] data, logger))
                    {
                        return false;
                    }

                    if (TryDeserializeObject(data, out result, logger))
                    {
                        logger.Info($"�ɹ�ʹ�� Unicode ���뷴���л�Ϊ {typeof(T).Name}");
                        logger.Info($"======Json.��ɼ����ļ� [{fileName}] ======");
                        return true;
                    }

                    // ����ʹ�� UTF-8 ����
                    if (TryDeserializeWithUtf8(data, out result, logger))
                    {
                        logger.Info($"ʹ�� UTF-8 ����ɹ������л� {typeof(T).Name}");
                        logger.Info($"======Json.��ɼ����ļ� [{fileName}] ======");
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// ���� <see cref=" JsonConvert"/> ���λ�� <see cref="ZipPath"/> Ŀ¼����Ϊ <paramref name="ZipFileName"/> �� <see cref="ZipFile"/> �ж�ȡ��������л�Ϊ <paramref name="RefObject"/> ��json�ļ�
        /// </summary>
        public static bool TryLoadJsonFromZip<T>(
            string ZipFileName,
            out T RefObject,
            T Default = default
        )
        {
            return TryLoadJsonFromZip(ZipFileName, out RefObject, DEFAULT_JSON_FILE_NAME, Default);
        }

        /// <summary>
        /// ���� <see cref="JsonConvert"/> ��� <see cref="ZipPath"/> Ŀ¼���ļ���Ϊ <paramref name="ZipFileName"/> �� Zip �ļ��г��Զ�ȡ <typeparamref name="T"/> ���͵Ŀ����л�����
        /// </summary>
        public static bool TryLoadJsonFromZip<T>(
            string zipFileName,
            out T result,
            string jsonName = "Default.json",
            T defaultValue = default,
            bool output = true
        )
        {
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                result = defaultValue;
                string fullPath = Path.Combine(ZipPath, zipFileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"�ļ�������: {fullPath}");
                    return false;
                }

                try
                {
                    // ʹ�� FileShare.ReadWrite �����������̷���
                    using (
                        var fileStream = new FileStream(
                            fullPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite
                        )
                    )
                    using (var zipStream = new ZipInputStream(fileStream))
                    {
                        ZipEntry entry;
                        while ((entry = zipStream.GetNextEntry()) != null)
                        {
                            if (entry.Name.Equals(jsonName, StringComparison.OrdinalIgnoreCase))
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    byte[] buffer = new byte[4096];
                                    int read;

                                    while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        memoryStream.Write(buffer, 0, read);
                                    }

                                    string json = Encoding.Unicode.GetString(
                                        memoryStream.ToArray()
                                    );
                                    result = JsonConvert.DeserializeObject<T>(json);

                                    logger.Info($"�ɹ��� {zipFileName} ���� {jsonName}");
                                    return true;
                                }
                            }
                        }

                        logger.Warning($"�� {zipFileName} ��δ�ҵ� {jsonName}");
                        return false;
                    }
                }
                catch (IOException ioEx)
                {
                    logger.Exception(ioEx);
                    logger.Error($"�ļ����ʴ���: {ioEx.Message}");
                    return false;
                }
                catch (JsonException jsonEx)
                {
                    logger.Exception(jsonEx);
                    logger.Error($"JSON ��������: {jsonEx.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"δ֪����: {ex.Message}");
                    return false;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// ���Զ�ȡ ZIP �ļ�����Ϊ�ֽ�����
        /// </summary>
        public static bool TryReadZipContentBytes(
            string fullPath,
            out byte[] data,
            AsyncLogger logger
        )
        {
            data = null;

            try
            {
                using (
                    var fileStream = new FileStream(
                        fullPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite
                    )
                )
                using (var zipStream = new ZipInputStream(fileStream))
                {
                    ZipEntry entry = zipStream.GetNextEntry();
                    if (entry == null)
                    {
                        logger.Error($"ZIP �ļ���û����Ŀ: {fullPath}");
                        return false;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[4096];
                        int read;

                        while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memoryStream.Write(buffer, 0, read);
                        }

                        data = memoryStream.ToArray();
                        logger.Info($"�ɹ���ȡ ZIP �ļ����ݣ���С: {data.Length} �ֽ�");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                return false;
            }
        }

        /// <summary>
        /// ���Զ�ȡ ZIP �ļ�����Ϊ�ַ���
        /// </summary>
        public static bool TryReadZipContent(string fullPath, out string json, AsyncLogger logger)
        {
            json = null;

            if (!TryReadZipContentBytes(fullPath, out byte[] data, logger))
            {
                return false;
            }

            // ����ʹ�� Unicode ����
            try
            {
                json = Encoding.Unicode.GetString(data);
                logger.Info($"�ɹ���ȡ ZIP �ļ�����Ϊ�ַ���������: {json.Length} �ַ�");
                return true;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                return false;
            }
        }

        /// <summary>
        /// ���Է����л� JSON �ַ���Ϊ����
        /// </summary>
        protected static bool TryDeserializeObject<T>(byte[] data, out T result, AsyncLogger logger)
        {
            result = default;

            try
            {
                string jsonUnicode = Encoding.Unicode.GetString(data);
                result = JsonConvert.DeserializeObject<T>(jsonUnicode, Settings);

                if (result != null)
                {
                    logger.Info($"�ɹ������л�Ϊ {typeof(T).Name}");
                    return true;
                }
                else
                {
                    logger.Warning($"�����л����Ϊ null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"�޷������л�Ϊ {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ����ʹ�� UTF-8 ���뷴���л�
        /// </summary>
        protected static bool TryDeserializeWithUtf8<T>(
            byte[] data,
            out T result,
            AsyncLogger logger
        )
        {
            result = default;

            try
            {
                string jsonUtf8 = Encoding.UTF8.GetString(data);
                result = JsonConvert.DeserializeObject<T>(jsonUtf8, Settings);

                if (result != null)
                {
                    logger.Info($"ʹ�� UTF-8 ����ɹ������л� {typeof(T).Name}");
                    return true;
                }
                else
                {
                    logger.Warning($"ʹ�� UTF-8 ���뷴���л����Ϊ null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                return false;
            }
        }

        #endregion

        #region StreamingAssets Support

        /// <summary>
        /// ��StreamingAssets�����ļ����־û�·��
        /// </summary>
        public static async UniTask<bool> TryCopyFromStreamingAssetsAsync(
            string sourceFileName,
            string destFileName,
            AsyncLogger logger = null,
            CancellationToken cancellationToken = default
        )
        {
            bool shouldDisposeLogger = logger == null;
            logger ??= new AsyncLogger(nameof(BaseJsonLoader), true);

            try
            {
                string sourcePath = Path.Combine(StreamingAssetsPath, sourceFileName);
                string destPath = Path.Combine(ZipPath, destFileName);

                // ȷ��Ŀ��Ŀ¼����
                string destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

#if UNITY_ANDROID || UNITY_WEBGL
                // ����Android��WebGLƽ̨ʹ��UnityWebRequest
                using (UnityWebRequest www = UnityWebRequest.Get(sourcePath))
                {
                    await www.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        logger.Error($"��StreamingAssets�����ļ�ʧ��: {www.error}");
                        return false;
                    }

                    byte[] data = www.downloadHandler.data;
                    await File.WriteAllBytesAsync(destPath, data, cancellationToken);
                }
#else
                // ����ƽֱ̨���ļ�����
                if (!File.Exists(sourcePath))
                {
                    logger.Error($"StreamingAssets���ļ�������: {sourcePath}");
                    return false;
                }

                await UniTask.RunOnThreadPool(
                    () =>
                    {
                        File.Copy(sourcePath, destPath, true);
                    },
                    cancellationToken: cancellationToken
                );
#endif

                logger.Info($"�ɹ���StreamingAssets�����ļ���: {destPath}");
                return true;
            }
            catch (OperationCanceledException)
            {
                logger.Warning("�ļ����Ʋ�����ȡ��");
                return false;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"��StreamingAssets�����ļ�ʧ��: {ex.Message}");
                return false;
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        /// <summary>
        /// ȷ���ļ����ڣ�������������StreamingAssets����
        /// </summary>
        public static async UniTask<bool> EnsureFileExistsAsync(
            string fileName,
            AsyncLogger logger = null,
            CancellationToken cancellationToken = default
        )
        {
            string filePath = Path.Combine(ZipPath, fileName);

            if (File.Exists(filePath))
            {
                return true;
            }

            return await TryCopyFromStreamingAssetsAsync(
                fileName,
                fileName,
                logger,
                cancellationToken
            );
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ȷ���ļ�ϵͳ�ѳ�ʼ��
        /// </summary>
        public static async UniTask<bool> EnsureInitializedAsync(
            bool forceReinitialize = false,
            CancellationToken cancellationToken = default
        )
        {
            return await FileInitializationManager.InitializeFilesAsync(
                forceReinitialize,
                cancellationToken
            );
        }

        #endregion

        #region Enhanced Initialization

        /// <summary>
        /// ���Զ���ʼ����StreamingAssets���˵ļ��ط���
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadWithInitializationAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // ȷ���Ѿ���ʼ��������StreamingAssets���ˣ�
            bool initialized = await EnsureInitializedAsync(false, cancellationToken);
            if (!initialized)
            {
                if (output)
                {
                    using (var logger = new AsyncLogger(nameof(BaseJsonLoader), true))
                    {
                        logger.Error("�ļ���ʼ��ʧ�ܣ��޷������ļ�");
                    }
                }
                return (false, default);
            }

            // ִ�м��أ�����StreamingAssets���ˣ�
            return await TryLoadObjectFromJsonAsync<T>(fileName, output, cancellationToken);
        }

        /// <summary>
        /// ���Զ���ʼ����StreamingAssets���˵�ZIP���ط���
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadZipWithInitializationAsync<T>(
            string zipFileName,
            string jsonName = DEFAULT_JSON_FILE_NAME,
            T defaultValue = default,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // ȷ���Ѿ���ʼ��������StreamingAssets���ˣ�
            bool initialized = await EnsureInitializedAsync(false, cancellationToken);
            if (!initialized)
            {
                if (output)
                {
                    using (var logger = new AsyncLogger(nameof(BaseJsonLoader), true))
                    {
                        logger.Error("�ļ���ʼ��ʧ�ܣ��޷������ļ�");
                    }
                }
                return (false, defaultValue);
            }

            // ִ�м��أ�����StreamingAssets���ˣ�
            return await TryLoadJsonFromZipAsync<T>(
                zipFileName,
                jsonName,
                defaultValue,
                output,
                cancellationToken
            );
        }

        #endregion
    }
}
