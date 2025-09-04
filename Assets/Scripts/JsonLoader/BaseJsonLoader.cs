using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace JsonLoader
{
    /// <summary>
    /// �����ѹ <see cref="ZipFile"/> �����н��� <see cref="Newtonsoft.Json"/> �ļ���
    /// ������Ϊ���������࣬��������ƻ�������ȡ������
    /// </summary>
    public class BaseJsonLoader
    {
        public const string DEFAULT_JSON_FILE_NAME = "Default.json";

        public static readonly string ZipPath = Application.persistentDataPath;

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
            byte[] buffer = Encoding.Unicode.GetBytes(content);
            string fullPath = Path.Combine(ZipPath, zipFileName);

            try
            {
                // ȷ��Ŀ¼����
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // ʹ�� FileShare.ReadWrite �����������̷���
                using (
                    var fileStream = new FileStream(
                        fullPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.ReadWrite
                    )
                ) // �����������̶�ȡ

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

                LogManager.Info($"�ɹ����� Zip �ļ�: {fullPath}", nameof(BaseJsonLoader), output);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Exception(ex, nameof(BaseJsonLoader), output);
                Debug.LogError($"���� Zip �ļ�ʧ��: {ex.Message}");
                return false;
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
            try
            {
                string Json = JsonConvert.SerializeObject(serializeObject, settings);
                return TrySaveToZip(zipFileName, Json);
            }
            catch (JsonReaderException)
            {
                Debug.LogWarning($"����ʱ��⵽ {zipFileName} �ļ��е� {JsonName} �﷨�ṹ����");
                return false;
            }
            catch (JsonSerializationException)
            {
                Debug.LogWarning(
                    $"����ʱ��⵽ {zipFileName} �ļ��е� {JsonName} ��������ת������"
                );
                return false;
            }
            catch (Exception)
            {
                Debug.LogWarning($"����ʱ��⵽ {zipFileName} �ļ��е� {JsonName} ��������");
                return false;
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
            return TryLoadJsonFromZip(ZipFileName, out RefObject, "Default.json", Default);
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
            result = defaultValue;
            string fullPath = Path.Combine(ZipPath, zipFileName);

            if (!File.Exists(fullPath))
            {
                LogManager.Error($"�ļ�������: {fullPath}", nameof(BaseJsonLoader), output);
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

                                string json = Encoding.Unicode.GetString(memoryStream.ToArray());
                                result = JsonConvert.DeserializeObject<T>(json);

                                LogManager.Info(
                                    $"�ɹ��� {zipFileName} ���� {jsonName}",
                                    nameof(BaseJsonLoader),
                                    output
                                );
                                return true;
                            }
                        }
                    }

                    LogManager.Warning(
                        $"�� {zipFileName} ��δ�ҵ� {jsonName}",
                        nameof(BaseJsonLoader),
                        output
                    );
                    return false;
                }
            }
            catch (IOException ioEx)
            {
                LogManager.Exception(ioEx, nameof(BaseJsonLoader), output);
                LogManager.Error($"�ļ����ʴ���: {ioEx.Message}", nameof(BaseJsonLoader), output);
                return false;
            }
            catch (JsonException jsonEx)
            {
                LogManager.Exception(jsonEx, nameof(BaseJsonLoader), output);
                LogManager.Error(
                    $"JSON ��������: {jsonEx.Message}",
                    nameof(BaseJsonLoader),
                    output
                );
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Exception(ex, nameof(BaseJsonLoader), output);
                LogManager.Error($"δ֪����: {ex.Message}", nameof(BaseJsonLoader), output);
                return false;
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
                return result != null;
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
                return result != null;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                return false;
            }
        }

        #endregion
    }
}
