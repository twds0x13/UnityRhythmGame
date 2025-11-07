using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using JsonLoader.JsonLoader;
using Newtonsoft.Json;
using UnityEngine;

namespace JsonLoader
{
    /// <summary>
    /// JSON编码格式
    /// </summary>
    public enum JsonEncoding
    {
        UTF8,
        Unicode,
        UTF32,
        ASCII,
    }

    namespace JsonLoader
    {
        /// <summary>
        /// 管理文件的初始化和检查
        /// </summary>
        public static class FileInitializationManager
        {
            private static readonly string[] REQUIRED_FILES = new string[] { "story.zip" };

            /// <summary>
            /// 检查并执行文件初始化
            /// </summary>
            /// <param name="cancellationToken">取消令牌</param>
            /// <param name="logger">异步日志记录器，如果为null则创建新的</param>
            /// <returns>初始化是否成功</returns>
            public static async UniTask<bool> InitializeFilesAsync(
                CancellationToken cancellationToken = default,
                AsyncLogger logger = null
            )
            {
                bool shouldDisposeLogger = logger == null;
                logger ??= new AsyncLogger(nameof(FileInitializationManager), true);

                try
                {
                    logger.Info("开始文件检查过程...");

                    // 检查并创建目录
                    EnsureDirectoryExists(logger);

                    // 检查必要文件是否存在，如果不存在则尝试从StreamingAssets复制
                    bool success = await CheckAndCopyRequiredFilesAsync(logger, cancellationToken);

                    if (success)
                    {
                        logger.Info("文件检查完成");
                    }
                    else
                    {
                        logger.Error("文件检查失败 - 必要文件不存在且无法从StreamingAssets复制");
                    }

                    return success;
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("文件检查被取消");
                    return false;
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    logger.Error($"文件检查异常: {ex.Message}");
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

                        bool copySuccess = await BaseJsonLoader.CopyFromStreamingAssetsAsync(
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
            /// 确保目录存在
            /// </summary>
            /// <param name="logger">异步日志记录器</param>
            private static void EnsureDirectoryExists(AsyncLogger logger)
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

            /// <summary>
            /// 获取文件信息统计
            /// </summary>
            /// <returns>文件信息统计对象</returns>
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
    }

    /// <summary>
    /// 文件信息统计
    /// </summary>
    public class FileInfoStats
    {
        /// <summary>总文件数</summary>
        public int TotalFiles { get; set; }

        /// <summary>总文件大小（字节）</summary>
        public long TotalSize { get; set; }

        /// <summary>文件详情列表</summary>
        public List<FileDetail> FileDetails { get; set; } = new List<FileDetail>();
    }

    /// <summary>
    /// 文件详情
    /// </summary>
    public class FileDetail
    {
        /// <summary>文件名</summary>
        public string FileName { get; set; }

        /// <summary>文件大小（字节）</summary>
        public long Size { get; set; }

        /// <summary>最后修改时间</summary>
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// 统一的加载配置
    /// </summary>
    public class LoadConfig
    {
        public bool OutputLog { get; set; } = true;
        public JsonEncoding Encoding { get; set; } = JsonEncoding.UTF8;
        public bool SearchSubdirectories { get; set; } = true;
        public bool AutoInitialize { get; set; } = true;
        public AsyncLogger Logger { get; set; }
        public CancellationToken CancellationToken { get; set; } = default;
    }

    /// <summary>
    /// 负责解压 ZipFile 并从中解析 Newtonsoft.Json 文件。
    /// 可以作为基础功能类，由子类客制化其他读取方法。
    /// </summary>
    public class BaseJsonLoader
    {
        /// <summary>默认JSON文件名</summary>
        public const string DEFAULT_JSON_FILE_NAME = "Default.json";

        /// <summary>ZIP文件存储路径</summary>
        public static readonly string ZipPath = Application.persistentDataPath;

        /// <summary>StreamingAssets路径</summary>
        public static readonly string StreamingAssetsPath = Application.streamingAssetsPath;

        /// <summary>
        /// JSON序列化设置
        /// 非常重要：如果不手动设置，会在反序列化时丢失组件的相关信息
        /// </summary>
        public static JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public static class BaseSerializer
        {
            public static string Serialize(object obj)
            {
                return JsonConvert.SerializeObject(obj, Settings);
            }

            public static T Deserialize<T>(string json)
            {
                return JsonConvert.DeserializeObject<T>(json, Settings);
            }

            public static object Deserialize(string json, Type type)
            {
                return JsonConvert.DeserializeObject(json, type, Settings);
            }
        }

        #region Save Methods

        /// <summary>
        /// 将可序列化对象保存到ZIP文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="serializeObject">要序列化的对象</param>
        /// <param name="config">加载配置</param>
        /// <returns>保存是否成功</returns>
        public static bool SaveObject<T>(
            string zipFileName,
            T serializeObject,
            LoadConfig config = null
        )
        {
            config ??= new LoadConfig();
            bool shouldDisposeLogger = config.Logger == null;
            var logger = config.Logger ?? new AsyncLogger(nameof(BaseJsonLoader), config.OutputLog);

            try
            {
                logger.Info($"======Json.开始保存文件 [{zipFileName}] ======");

                string json = JsonConvert.SerializeObject(serializeObject, Settings);
                logger.Info($"对象已序列化，JSON 长度: {json.Length} 字符");

                bool success = SaveZipInternal(zipFileName, json, logger, config.Encoding);

                if (success)
                {
                    logger.Info($"======Json.完成保存文件 [{zipFileName}] ======");
                }
                else
                {
                    logger.Error($"======Json.保存文件失败 [{zipFileName}] ======");
                }

                return success;
            }
            catch (JsonReaderException ex)
            {
                logger.Exception(ex);
                logger.Error($"保存时检测到 {zipFileName} 文件语法结构错误: {ex.Message}");
                return false;
            }
            catch (JsonSerializationException ex)
            {
                logger.Exception(ex);
                logger.Error($"保存时检测到 {zipFileName} 文件包含类型转化错误: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"保存时检测到 {zipFileName} 文件解析错误: {ex.Message}");
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
        /// 异步保存对象到 ZIP 文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="serializeObject">要序列化的对象</param>
        /// <param name="config">加载配置</param>
        /// <returns>保存是否成功</returns>
        public static async UniTask<bool> SaveObjectAsync<T>(
            string zipFileName,
            T serializeObject,
            LoadConfig config = null
        )
        {
            config ??= new LoadConfig();
            bool shouldDisposeLogger = config.Logger == null;
            var logger = config.Logger ?? new AsyncLogger(nameof(BaseJsonLoader), config.OutputLog);

            try
            {
                logger.Info($"开始异步保存文件 [{zipFileName}]");

                return await UniTask.RunOnThreadPool(
                    () => SaveObject(zipFileName, serializeObject, config),
                    cancellationToken: config.CancellationToken
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
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        #endregion

        #region Load Methods

        /// <summary>
        /// 统一的加载方法 - 从ZIP文件加载JSON并反序列化为对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="jsonName">JSON文件名</param>
        /// <param name="config">加载配置</param>
        /// <returns>包含成功状态和结果的元组</returns>
        public static (bool success, T result) LoadObject<T>(
            string zipFileName,
            string jsonName = DEFAULT_JSON_FILE_NAME,
            LoadConfig config = null
        )
        {
            config ??= new LoadConfig();
            bool shouldDisposeLogger = config.Logger == null;
            var logger = config.Logger ?? new AsyncLogger(nameof(BaseJsonLoader), config.OutputLog);

            try
            {
                logger.Info($"======Json.开始加载文件 [{zipFileName}] ======");

                string fullPath = Path.Combine(ZipPath, zipFileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
                    return (false, default);
                }

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
                    bool found = false;
                    T result = default;

                    while ((entry = zipStream.GetNextEntry()) != null)
                    {
                        // 检查文件名匹配（支持子目录搜索）
                        bool nameMatches = CheckEntryNameMatch(
                            entry.Name,
                            jsonName,
                            config.SearchSubdirectories
                        );

                        if (nameMatches)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                byte[] buffer = new byte[4096];
                                int read;

                                while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    memoryStream.Write(buffer, 0, read);
                                }

                                byte[] data = memoryStream.ToArray();
                                if (
                                    TryDeserializeData<T>(data, out result, logger, config.Encoding)
                                )
                                {
                                    logger.Info($"成功从 {zipFileName} 加载 {entry.Name}");
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        logger.Warning($"在 {zipFileName} 中未找到 {jsonName}");
                    }

                    logger.Info($"======Json.完成加载文件 [{zipFileName}] ======");
                    return (found, result);
                }
            }
            catch (IOException ioEx)
            {
                logger.Exception(ioEx);
                logger.Error($"文件访问错误: {ioEx.Message}");
                return (false, default);
            }
            catch (JsonException jsonEx)
            {
                logger.Exception(jsonEx);
                logger.Error($"JSON 解析错误: {jsonEx.Message}");
                return (false, default);
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"加载失败: {ex.Message}");
                return (false, default);
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
        /// 统一的异步加载方法
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="jsonName">JSON文件名</param>
        /// <param name="config">加载配置</param>
        /// <returns>包含成功状态和结果的元组</returns>
        public static async UniTask<(bool success, T result)> LoadObjectAsync<T>(
            string zipFileName,
            string jsonName = DEFAULT_JSON_FILE_NAME,
            LoadConfig config = null
        )
        {
            config ??= new LoadConfig();
            bool shouldDisposeLogger = config.Logger == null;
            var logger = config.Logger ?? new AsyncLogger(nameof(BaseJsonLoader), config.OutputLog);

            try
            {
                logger.Info($"开始异步加载文件 [{zipFileName}]");

                // 如果需要自动初始化
                if (config.AutoInitialize)
                {
                    bool initialized = await EnsureInitializedAsync(
                        config.CancellationToken,
                        logger
                    );
                    if (!initialized)
                    {
                        logger.Error("文件初始化失败，无法加载文件");
                        return (false, default);
                    }
                }

                return await UniTask.RunOnThreadPool(
                    () => LoadObject<T>(zipFileName, jsonName, config),
                    cancellationToken: config.CancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                logger.Warning($"文件加载被取消: {zipFileName}");
                return (false, default);
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"异步加载失败: {ex.Message}");
                return (false, default);
            }
            finally
            {
                if (shouldDisposeLogger)
                {
                    logger.Dispose();
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 根据编码枚举获取对应的Encoding对象
        /// </summary>
        public static Encoding GetEncoding(JsonEncoding encoding)
        {
            return encoding switch
            {
                JsonEncoding.UTF8 => Encoding.UTF8,
                JsonEncoding.Unicode => Encoding.Unicode,
                JsonEncoding.UTF32 => Encoding.UTF32,
                JsonEncoding.ASCII => Encoding.ASCII,
                _ => Encoding.UTF8,
            };
        }

        /// <summary>
        /// 检查ZIP条目名称是否匹配
        /// </summary>
        private static bool CheckEntryNameMatch(
            string entryName,
            string targetName,
            bool searchSubdirectories
        )
        {
            // 精确匹配
            if (entryName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 如果启用子目录搜索，检查文件名部分
            if (searchSubdirectories)
            {
                string fileName = Path.GetFileName(entryName);
                return fileName.Equals(targetName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// 内部保存ZIP文件方法
        /// </summary>
        private static bool SaveZipInternal(
            string zipFileName,
            string content,
            AsyncLogger logger,
            JsonEncoding encoding = JsonEncoding.UTF8
        )
        {
            try
            {
                Encoding textEncoding = GetEncoding(encoding);
                byte[] buffer = textEncoding.GetBytes(content);
                string fullPath = Path.Combine(ZipPath, zipFileName);

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

                logger.Info($"成功保存 Zip 文件: {fullPath} (编码: {encoding})");
                return true;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"保存 Zip 文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 统一的JSON数据反序列化方法
        /// </summary>
        private static bool TryDeserializeData<T>(
            byte[] data,
            out T result,
            AsyncLogger logger,
            JsonEncoding encoding = JsonEncoding.UTF8
        )
        {
            result = default;

            // 首先尝试指定编码
            if (TryDeserializeWithEncoding(data, out result, logger, encoding))
            {
                return true;
            }

            // 如果失败，尝试备用编码
            logger.Warning($"使用 {encoding} 编码反序列化失败，尝试备用编码...");
            return TryDeserializeWithFallbackEncodings(data, out result, logger);
        }

        /// <summary>
        /// 使用指定编码反序列化
        /// </summary>
        private static bool TryDeserializeWithEncoding<T>(
            byte[] data,
            out T result,
            AsyncLogger logger,
            JsonEncoding encoding
        )
        {
            result = default;

            try
            {
                Encoding textEncoding = GetEncoding(encoding);
                string json = textEncoding.GetString(data);
                result = JsonConvert.DeserializeObject<T>(json, Settings);

                if (result != null)
                {
                    logger.Info($"成功使用 {encoding} 编码反序列化为 {typeof(T).Name}");
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
                logger.Error($"无法使用 {encoding} 编码反序列化为 {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试使用备用编码反序列化
        /// </summary>
        private static bool TryDeserializeWithFallbackEncodings<T>(
            byte[] data,
            out T result,
            AsyncLogger logger
        )
        {
            result = default;

            // 尝试不同的编码
            JsonEncoding[] encodingsToTry =
            {
                JsonEncoding.UTF8,
                JsonEncoding.Unicode,
                JsonEncoding.UTF32,
                JsonEncoding.ASCII,
            };

            foreach (var encoding in encodingsToTry)
            {
                if (TryDeserializeWithEncoding(data, out result, logger, encoding))
                {
                    logger.Info($"使用备用编码 {encoding} 成功反序列化 {typeof(T).Name}");
                    return true;
                }
            }

            logger.Error("所有编码尝试都失败");
            return false;
        }

        #endregion

        #region StreamingAssets Support

        /// <summary>
        /// 从StreamingAssets复制文件到持久化路径
        /// </summary>
        /// <param name="sourceFileName">源文件名</param>
        /// <param name="destFileName">目标文件名</param>
        /// <param name="logger">异步日志记录器</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>复制是否成功</returns>
        public static async UniTask<bool> CopyFromStreamingAssetsAsync(
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

        #endregion

        #region Initialization

        /// <summary>
        /// 确保文件系统已初始化
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="logger">异步日志记录器</param>
        /// <returns>初始化是否成功</returns>
        public static async UniTask<bool> EnsureInitializedAsync(
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            return await FileInitializationManager.InitializeFilesAsync(cancellationToken, logger);
        }

        #endregion
    }
}
