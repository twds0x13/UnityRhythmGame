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
    /// 负责解压 <see cref="ZipFile"/> 并从中解析 <see cref="Newtonsoft.Json"/> 文件。
    /// 可以作为基础功能类，由子类客制化其他读取方法。
    /// </summary>
    public class BaseJsonLoader
    {
        public const string DEFAULT_JSON_FILE_NAME = "Default.json";

        public static readonly string ZipPath = Application.persistentDataPath;

        // 非常重要：如果不手动设置，会在反序列化时丢失组件的相关信息
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
                // 确保目录存在
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 使用 FileShare.ReadWrite 允许其他进程访问
                using (
                    var fileStream = new FileStream(
                        fullPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.ReadWrite
                    )
                ) // 允许其他进程读取

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

                LogManager.Info($"成功保存 Zip 文件: {fullPath}", nameof(BaseJsonLoader), output);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Exception(ex, nameof(BaseJsonLoader), output);
                Debug.LogError($"保存 Zip 文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 调用 <see cref=" JsonConvert"/> 库将任意可序列化 <paramref name="serializeObject"/> 的已压缩 .json 文件存储到 <see cref="Application.persistentDataPath"/>。文件名为 <paramref name="zipFileName"/>
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
        /// 其实这个 <paramref name="JsonName"/> 只是为了用户看着舒服，因为 <see cref="LoadJsonFromZip{T}(string, ref T)"/> 不要求传入 <paramref name="JsonName"/>
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
                Debug.LogWarning($"保存时检测到 {zipFileName} 文件中的 {JsonName} 语法结构错误。");
                return false;
            }
            catch (JsonSerializationException)
            {
                Debug.LogWarning(
                    $"保存时检测到 {zipFileName} 文件中的 {JsonName} 包含类型转化错误。"
                );
                return false;
            }
            catch (Exception)
            {
                Debug.LogWarning($"保存时检测到 {zipFileName} 文件中的 {JsonName} 解析错误。");
                return false;
            }
        }

        /// <summary>
        /// 异步保存对象到 ZIP 文件
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
            // 创建异步日志记录器
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                logger.Info($"开始异步保存文件 [{zipFileName}]");

                try
                {
                    // 在线程池中执行序列化和保存操作
                    return await UniTask.RunOnThreadPool(
                        () =>
                        {
                            try
                            {
                                // 检查取消请求
                                cancellationToken.ThrowIfCancellationRequested();

                                // 序列化对象
                                string json = JsonConvert.SerializeObject(
                                    serializeObject,
                                    settings
                                );
                                logger.Info($"对象已序列化，JSON 长度: {json.Length} 字符");

                                // 保存到 ZIP 文件
                                bool saveSuccess = TrySaveToZip(zipFileName, json, false);

                                if (saveSuccess)
                                {
                                    logger.Info($"成功异步保存文件: {zipFileName}");
                                }
                                else
                                {
                                    logger.Error($"异步保存文件失败: {zipFileName}");
                                }

                                return saveSuccess;
                            }
                            catch (OperationCanceledException)
                            {
                                logger.Warning("保存操作被取消");
                                return false;
                            }
                            catch (JsonReaderException ex)
                            {
                                logger.Exception(ex);
                                logger.Error($"JSON 语法错误: {ex.Message}");
                                return false;
                            }
                            catch (JsonSerializationException ex)
                            {
                                logger.Exception(ex);
                                logger.Error($"JSON 序列化错误: {ex.Message}");
                                return false;
                            }
                            catch (Exception ex)
                            {
                                logger.Exception(ex);
                                logger.Error($"保存失败: {ex.Message}");
                                return false;
                            }
                        },
                        cancellationToken: cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("保存任务被取消");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"异步保存失败: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 异步保存对象到 ZIP 文件（简化版）
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
        /// 异步加载 JSON 文件并反序列化，使用 <see cref="AsyncLogger"/> 记录日志
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadObjectFromJsonAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // 创建异步日志记录器
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                logger.Info($"开始异步加载文件 [{fileName}]");

                T result = default;

                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
                    return (false, result);
                }

                try
                {
                    // 使用 UniTask.RunOnThreadPool 在后台线程执行同步操作
                    var loadResult = await UniTask.RunOnThreadPool(
                        () => TryLoadObjectFromJson(fileName, out result, false),
                        cancellationToken: cancellationToken
                    );

                    // 记录结果
                    if (loadResult)
                    {
                        logger.Info($"成功异步加载 {typeof(T).Name} 实体");
                    }
                    else
                    {
                        logger.Error("异步加载失败");
                    }

                    return (loadResult, result);
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("JSON 加载被取消");
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
        /// 调用 <see cref=" JsonConvert"/> 库从位于 <see cref="ZipPath"/> 目录下的 <paramref name="fileName"/> 文件反序列化出对象。
        /// </summary>
        public static bool TryLoadObjectFromJson<T>(
            string fileName,
            out T result,
            bool output = true
        )
        {
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                logger.Info($"======Json.开始加载文件 [{fileName}] ======");

                result = default;
                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
                    return false;
                }

                try
                {
                    // 读取 ZIP 文件内容为字节数组
                    if (!TryReadZipContentBytes(fullPath, out byte[] data, logger))
                    {
                        return false;
                    }

                    if (TryDeserializeObject(data, out result, logger))
                    {
                        logger.Info($"成功使用 Unicode 编码反序列化为 {typeof(T).Name}");
                        logger.Info($"======Json.完成加载文件 [{fileName}] ======");
                        return true;
                    }

                    // 尝试使用 UTF-8 编码
                    if (TryDeserializeWithUtf8(data, out result, logger))
                    {
                        logger.Info($"使用 UTF-8 编码成功反序列化 {typeof(T).Name}");
                        logger.Info($"======Json.完成加载文件 [{fileName}] ======");
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
        /// 调用 <see cref=" JsonConvert"/> 库从位于 <see cref="ZipPath"/> 目录下名为 <paramref name="ZipFileName"/> 的 <see cref="ZipFile"/> 中读取任意可序列化为 <paramref name="RefObject"/> 的json文件
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
        /// 调用 <see cref="JsonConvert"/> 库从 <see cref="ZipPath"/> 目录下文件名为 <paramref name="ZipFileName"/> 的 Zip 文件中尝试读取 <typeparamref name="T"/> 类型的可序列化对象
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
                LogManager.Error($"文件不存在: {fullPath}", nameof(BaseJsonLoader), output);
                return false;
            }

            try
            {
                // 使用 FileShare.ReadWrite 允许其他进程访问
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
                                    $"成功从 {zipFileName} 加载 {jsonName}",
                                    nameof(BaseJsonLoader),
                                    output
                                );
                                return true;
                            }
                        }
                    }

                    LogManager.Warning(
                        $"在 {zipFileName} 中未找到 {jsonName}",
                        nameof(BaseJsonLoader),
                        output
                    );
                    return false;
                }
            }
            catch (IOException ioEx)
            {
                LogManager.Exception(ioEx, nameof(BaseJsonLoader), output);
                LogManager.Error($"文件访问错误: {ioEx.Message}", nameof(BaseJsonLoader), output);
                return false;
            }
            catch (JsonException jsonEx)
            {
                LogManager.Exception(jsonEx, nameof(BaseJsonLoader), output);
                LogManager.Error(
                    $"JSON 解析错误: {jsonEx.Message}",
                    nameof(BaseJsonLoader),
                    output
                );
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Exception(ex, nameof(BaseJsonLoader), output);
                LogManager.Error($"未知错误: {ex.Message}", nameof(BaseJsonLoader), output);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 尝试读取 ZIP 文件内容为字节数组
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
                        logger.Error($"ZIP 文件中没有条目: {fullPath}");
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
        /// 尝试读取 ZIP 文件内容为字符串
        /// </summary>
        public static bool TryReadZipContent(string fullPath, out string json, AsyncLogger logger)
        {
            json = null;

            if (!TryReadZipContentBytes(fullPath, out byte[] data, logger))
            {
                return false;
            }

            // 尝试使用 Unicode 编码
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
        /// 尝试反序列化 JSON 字符串为对象
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
                logger.Error($"无法反序列化为 {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试使用 UTF-8 编码反序列化
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
