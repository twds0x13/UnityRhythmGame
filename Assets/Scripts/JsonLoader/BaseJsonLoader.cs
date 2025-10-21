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
    /// 管理文件的初始化和检查
    /// </summary>
    public static class FileInitializationManager
    {
        private const string INITIALIZATION_FLAG = "FilesInitialized";
        private static readonly string[] REQUIRED_FILES = new string[] { "story.zip" };

        /// <summary>
        /// 检查并执行文件初始化
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
                    // 检查是否已经初始化
                    if (!forceReinitialize && IsInitialized())
                    {
                        logger.Info("文件已经初始化，跳过初始化过程");
                        return true;
                    }

                    logger.Info("开始文件初始化过程...");

                    // 检查并创建目录
                    EnsureDirectoryExists();

                    // 检查必要文件是否存在，如果不存在则尝试从StreamingAssets复制
                    bool success = await CheckAndCopyRequiredFilesAsync(logger, cancellationToken);

                    if (success)
                    {
                        // 标记初始化完成
                        SetInitialized();
                        logger.Info("文件初始化完成");
                    }
                    else
                    {
                        logger.Error("文件初始化失败 - 必要文件不存在且无法从StreamingAssets复制");
                    }

                    return success;
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("文件初始化被取消");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"文件初始化异常: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 检查必要文件是否存在，如果不存在则从StreamingAssets复制
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
                    logger.Warning($"必要文件不存在: {filePath}，尝试从StreamingAssets复制");

                    bool copySuccess = await BaseJsonLoader.TryCopyFromStreamingAssetsAsync(
                        fileName,
                        fileName,
                        logger,
                        cancellationToken
                    );
                    if (!copySuccess)
                    {
                        logger.Error($"从StreamingAssets复制文件失败: {fileName}");
                        return false;
                    }

                    logger.Info($"成功从StreamingAssets复制文件: {fileName}");
                }
                else
                {
                    logger.Info($"必要文件存在: {filePath}");
                }
            }
            return true;
        }

        /// <summary>
        /// 检查文件是否已经初始化
        /// </summary>
        public static bool IsInitialized()
        {
            return PlayerPrefs.GetInt(INITIALIZATION_FLAG, 0) == 1;
        }

        /// <summary>
        /// 标记文件为已初始化
        /// </summary>
        public static void SetInitialized()
        {
            PlayerPrefs.SetInt(INITIALIZATION_FLAG, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 重置初始化状态（用于测试或重新初始化）
        /// </summary>
        public static void ResetInitialization()
        {
            PlayerPrefs.DeleteKey(INITIALIZATION_FLAG);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectoryExists()
        {
            using (var logger = new AsyncLogger(nameof(FileInitializationManager), true))
            {
                if (!Directory.Exists(BaseJsonLoader.ZipPath))
                {
                    Directory.CreateDirectory(BaseJsonLoader.ZipPath);
                    logger.Info($"创建目录: {BaseJsonLoader.ZipPath}");
                }
                else
                {
                    logger.Info($"目录已存在: {BaseJsonLoader.ZipPath}");
                }
            }
        }

        /// <summary>
        /// 获取文件信息统计
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
    /// 文件信息统计
    /// </summary>
    public class FileInfoStats
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public List<FileDetail> FileDetails { get; set; } = new List<FileDetail>();
    }

    /// <summary>
    /// 文件详情
    /// </summary>
    public class FileDetail
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// 负责解压 <see cref="ZipFile"/> 并从中解析 <see cref="Newtonsoft.Json"/> 文件。
    /// 可以作为基础功能类，由子类客制化其他读取方法。
    /// </summary>
    public class BaseJsonLoader
    {
        public const string DEFAULT_JSON_FILE_NAME = "Default.json";

        public static readonly string ZipPath = Application.persistentDataPath;
        public static readonly string StreamingAssetsPath = Application.streamingAssetsPath;

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
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
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
                        logger.Info($"创建目录: {directory}");
                    }

                    // 使用 FileShare.ReadWrite 允许其他进程访问
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

                    logger.Info($"成功保存 Zip 文件: {fullPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"保存 Zip 文件失败: {ex.Message}");
                    return false;
                }
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
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), true))
            {
                try
                {
                    string Json = JsonConvert.SerializeObject(serializeObject, settings);
                    logger.Info($"对象已序列化，JSON 长度: {Json.Length} 字符");
                    return TrySaveToZip(zipFileName, Json);
                }
                catch (JsonReaderException ex)
                {
                    logger.Exception(ex);
                    logger.Warning(
                        $"保存时检测到 {zipFileName} 文件中的 {JsonName} 语法结构错误。"
                    );
                    return false;
                }
                catch (JsonSerializationException ex)
                {
                    logger.Exception(ex);
                    logger.Warning(
                        $"保存时检测到 {zipFileName} 文件中的 {JsonName} 包含类型转化错误。"
                    );
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Warning($"保存时检测到 {zipFileName} 文件中的 {JsonName} 解析错误。");
                    return false;
                }
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
        /// 调用 <see cref=" JsonConvert"/> 库从位于 <see cref="ZipPath"/> 目录下名为 <paramref name="ZipFileName"/> 的 <see cref="ZipFile"/> 中读取任意可序列化为 <paramref name="RefObject"/> 的json文件
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
        /// 异步从 ZIP 文件加载 JSON 并反序列化
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
                logger.Info($"开始异步加载 ZIP 文件 [{zipFileName}] 中的 [{jsonName}]");

                try
                {
                    // 在线程池中执行同步加载操作
                    return await UniTask.RunOnThreadPool(
                        () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            bool success = TryLoadJsonFromZip(
                                zipFileName,
                                out T result,
                                jsonName,
                                defaultValue,
                                false // 在后台线程中不输出日志
                            );

                            if (success)
                            {
                                logger.Info(
                                    $"成功异步加载 ZIP 文件 [{zipFileName}] 中的 [{jsonName}]"
                                );
                            }
                            else
                            {
                                logger.Error($"异步加载 ZIP 文件失败: {zipFileName}");
                            }

                            return (success, result);
                        },
                        cancellationToken: cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    logger.Warning($"ZIP 文件加载被取消: {zipFileName}");
                    return (false, defaultValue);
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"异步加载 ZIP 文件异常: {ex.Message}");
                    return (false, defaultValue);
                }
            }
        }

        /// <summary>
        /// 异步从 ZIP 文件加载 JSON 并反序列化（简化版）
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
        /// 异步从 ZIP 文件加载 JSON 并反序列化（使用默认值）
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
            return TryLoadJsonFromZip(ZipFileName, out RefObject, DEFAULT_JSON_FILE_NAME, Default);
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
            using (var logger = new AsyncLogger(nameof(BaseJsonLoader), output))
            {
                result = defaultValue;
                string fullPath = Path.Combine(ZipPath, zipFileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
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

                                    string json = Encoding.Unicode.GetString(
                                        memoryStream.ToArray()
                                    );
                                    result = JsonConvert.DeserializeObject<T>(json);

                                    logger.Info($"成功从 {zipFileName} 加载 {jsonName}");
                                    return true;
                                }
                            }
                        }

                        logger.Warning($"在 {zipFileName} 中未找到 {jsonName}");
                        return false;
                    }
                }
                catch (IOException ioEx)
                {
                    logger.Exception(ioEx);
                    logger.Error($"文件访问错误: {ioEx.Message}");
                    return false;
                }
                catch (JsonException jsonEx)
                {
                    logger.Exception(jsonEx);
                    logger.Error($"JSON 解析错误: {jsonEx.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"未知错误: {ex.Message}");
                    return false;
                }
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
                        logger.Info($"成功读取 ZIP 文件内容，大小: {data.Length} 字节");
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
                logger.Info($"成功读取 ZIP 文件内容为字符串，长度: {json.Length} 字符");
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

                if (result != null)
                {
                    logger.Info($"成功反序列化为 {typeof(T).Name}");
                    return true;
                }
                else
                {
                    logger.Warning($"反序列化结果为 null");
                    return false;
                }
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

                if (result != null)
                {
                    logger.Info($"使用 UTF-8 编码成功反序列化 {typeof(T).Name}");
                    return true;
                }
                else
                {
                    logger.Warning($"使用 UTF-8 编码反序列化结果为 null");
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
        /// 从StreamingAssets复制文件到持久化路径
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

                // 确保目标目录存在
                string destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

#if UNITY_ANDROID || UNITY_WEBGL
                // 对于Android和WebGL平台使用UnityWebRequest
                using (UnityWebRequest www = UnityWebRequest.Get(sourcePath))
                {
                    await www.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        logger.Error($"从StreamingAssets下载文件失败: {www.error}");
                        return false;
                    }

                    byte[] data = www.downloadHandler.data;
                    await File.WriteAllBytesAsync(destPath, data, cancellationToken);
                }
#else
                // 其他平台直接文件操作
                if (!File.Exists(sourcePath))
                {
                    logger.Error($"StreamingAssets中文件不存在: {sourcePath}");
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

                logger.Info($"成功从StreamingAssets复制文件到: {destPath}");
                return true;
            }
            catch (OperationCanceledException)
            {
                logger.Warning("文件复制操作被取消");
                return false;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"从StreamingAssets复制文件失败: {ex.Message}");
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
        /// 确保文件存在，如果不存在则从StreamingAssets复制
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
        /// 确保文件系统已初始化
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
        /// 带自动初始化和StreamingAssets回退的加载方法
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadWithInitializationAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // 确保已经初始化（包含StreamingAssets回退）
            bool initialized = await EnsureInitializedAsync(false, cancellationToken);
            if (!initialized)
            {
                if (output)
                {
                    using (var logger = new AsyncLogger(nameof(BaseJsonLoader), true))
                    {
                        logger.Error("文件初始化失败，无法加载文件");
                    }
                }
                return (false, default);
            }

            // 执行加载（包含StreamingAssets回退）
            return await TryLoadObjectFromJsonAsync<T>(fileName, output, cancellationToken);
        }

        /// <summary>
        /// 带自动初始化和StreamingAssets回退的ZIP加载方法
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadZipWithInitializationAsync<T>(
            string zipFileName,
            string jsonName = DEFAULT_JSON_FILE_NAME,
            T defaultValue = default,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // 确保已经初始化（包含StreamingAssets回退）
            bool initialized = await EnsureInitializedAsync(false, cancellationToken);
            if (!initialized)
            {
                if (output)
                {
                    using (var logger = new AsyncLogger(nameof(BaseJsonLoader), true))
                    {
                        logger.Error("文件初始化失败，无法加载文件");
                    }
                }
                return (false, defaultValue);
            }

            // 执行加载（包含StreamingAssets回退）
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
