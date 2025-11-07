using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonLoader
{
    /// <summary>
    /// 音游谱面数据管理器 - 负责谱面相关的JSON文件操作
    /// </summary>
    public sealed class ChartJsonLoader : BaseJsonLoader
    {
        #region 配置常量

        /// <summary>
        /// 默认谱面配置
        /// </summary>
        private static LoadConfig DefaultConfig =>
            new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.UTF8,
                SearchSubdirectories = true,
                AutoInitialize = true,
            };

        #endregion

        #region 核心加载方法

        /// <summary>
        /// 从 Zip 文件中加载谱面数据
        /// </summary>
        /// <typeparam name="T">要加载的对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="config">加载配置</param>
        /// <returns>包含成功状态和结果的元组</returns>
        public static (bool success, T result) Load<T>(string zipFileName, LoadConfig config = null)
        {
            config ??= DefaultConfig;
            return LoadObject<T>(zipFileName, DEFAULT_JSON_FILE_NAME, config);
        }

        /// <summary>
        /// 异步从 ZIP 文件加载谱面数据
        /// </summary>
        /// <typeparam name="T">要加载的对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="config">加载配置</param>
        /// <returns>反序列化的对象</returns>
        public static async UniTask<T> LoadAsync<T>(string zipFileName, LoadConfig config = null)
        {
            config ??= DefaultConfig;
            var (success, result) = await LoadObjectAsync<T>(
                zipFileName,
                DEFAULT_JSON_FILE_NAME,
                config
            );
            return result;
        }

        #endregion

        #region 保存方法

        /// <summary>
        /// 保存谱面数据到 ZIP 文件
        /// </summary>
        /// <typeparam name="T">要保存的对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="chartData">谱面数据</param>
        /// <param name="config">加载配置</param>
        /// <returns>保存是否成功</returns>
        public static bool Save<T>(string zipFileName, T chartData, LoadConfig config = null)
        {
            config ??= DefaultConfig;
            return SaveObject(zipFileName, chartData, config);
        }

        /// <summary>
        /// 异步保存谱面数据到 ZIP 文件
        /// </summary>
        /// <typeparam name="T">要保存的对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="chartData">谱面数据</param>
        /// <param name="config">加载配置</param>
        /// <returns>保存是否成功</returns>
        public static async UniTask<bool> SaveAsync<T>(
            string zipFileName,
            T chartData,
            LoadConfig config = null
        )
        {
            config ??= DefaultConfig;
            return await SaveObjectAsync(zipFileName, chartData, config);
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 快速加载谱面数据（使用默认配置）
        /// </summary>
        /// <typeparam name="T">要加载的对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="logger">异步日志记录器</param>
        /// <returns>反序列化的对象</returns>
        public static async UniTask<T> LoadQuickAsync<T>(
            string zipFileName,
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            var config = new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.UTF8,
                AutoInitialize = true,
                CancellationToken = cancellationToken,
                Logger = logger,
            };

            return await LoadAsync<T>(zipFileName, config);
        }

        /// <summary>
        /// 快速保存谱面数据（使用默认配置）
        /// </summary>
        /// <typeparam name="T">要保存的对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="chartData">谱面数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="logger">异步日志记录器</param>
        /// <returns>保存是否成功</returns>
        public static async UniTask<bool> SaveQuickAsync<T>(
            string zipFileName,
            T chartData,
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            var config = new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.UTF8,
                CancellationToken = cancellationToken,
                Logger = logger,
            };

            return await SaveAsync(zipFileName, chartData, config);
        }

        /// <summary>
        /// 同步快速加载谱面数据
        /// </summary>
        /// <typeparam name="T">要加载的对象类型</typeparam>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <param name="logger">异步日志记录器</param>
        /// <returns>包含成功状态和结果的元组</returns>
        public static (bool success, T result) LoadQuick<T>(
            string zipFileName,
            AsyncLogger logger = null
        )
        {
            var config = new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.UTF8,
                AutoInitialize = false, // 同步方法不自动初始化
                Logger = logger,
            };

            return Load<T>(zipFileName, config);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 验证谱面JSON结构
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>验证是否成功</returns>
        public static bool ValidateChartStructure(string json, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var jToken = JToken.Parse(json);

                // 基本的谱面结构验证
                if (jToken.Type != JTokenType.Object)
                {
                    errorMessage = "谱面JSON应该是对象格式";
                    return false;
                }

                // 可以添加更多的谱面特定验证逻辑
                var obj = jToken as JObject;
                if (!obj.ContainsKey("notes") || !obj.ContainsKey("timingPoints"))
                {
                    errorMessage = "谱面JSON缺少必要的字段 (notes, timingPoints)";
                    return false;
                }

                return true;
            }
            catch (JsonReaderException ex)
            {
                errorMessage = $"JSON解析错误: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"验证错误: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 检查谱面文件是否存在
        /// </summary>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <returns>文件是否存在</returns>
        public static bool ChartExists(string zipFileName)
        {
            string fullPath = Path.Combine(ZipPath, zipFileName);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// 获取谱面文件信息
        /// </summary>
        /// <param name="zipFileName">ZIP文件名</param>
        /// <returns>文件信息</returns>
        public static FileInfo GetChartFileInfo(string zipFileName)
        {
            string fullPath = Path.Combine(ZipPath, zipFileName);
            return new FileInfo(fullPath);
        }

        #endregion

        #region 高级配置方法

        /// <summary>
        /// 创建谱面专用配置
        /// </summary>
        /// <param name="outputLog">是否输出日志</param>
        /// <param name="encoding">编码格式</param>
        /// <param name="autoInitialize">是否自动初始化</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="logger">异步日志记录器</param>
        /// <returns>加载配置</returns>
        public static LoadConfig CreateChartConfig(
            bool outputLog = true,
            JsonEncoding encoding = JsonEncoding.UTF8,
            bool autoInitialize = true,
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            return new LoadConfig
            {
                OutputLog = outputLog,
                Encoding = encoding,
                AutoInitialize = autoInitialize,
                SearchSubdirectories = true,
                CancellationToken = cancellationToken,
                Logger = logger,
            };
        }

        /// <summary>
        /// 创建高性能配置（无日志，不自动初始化）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>加载配置</returns>
        public static LoadConfig CreatePerformanceConfig(
            CancellationToken cancellationToken = default
        )
        {
            return new LoadConfig
            {
                OutputLog = false,
                Encoding = JsonEncoding.UTF8,
                AutoInitialize = false,
                SearchSubdirectories = true,
                CancellationToken = cancellationToken,
            };
        }

        #endregion

        #region 初始化相关

        /// <summary>
        /// 确保谱面文件已初始化
        /// </summary>
        /// <param name="config">加载配置</param>
        /// <returns>初始化是否成功</returns>
        public static async UniTask<bool> EnsureInitializedAsync(LoadConfig config = null)
        {
            config ??= DefaultConfig;
            bool shouldDisposeLogger = config.Logger == null;
            var logger =
                config.Logger ?? new AsyncLogger(nameof(ChartJsonLoader), config.OutputLog);

            try
            {
                logger.Info("开始初始化谱面文件...");

                bool initialized = await BaseJsonLoader.EnsureInitializedAsync(
                    config.CancellationToken,
                    logger
                );

                if (initialized)
                {
                    logger.Info("谱面文件初始化完成");
                }
                else
                {
                    logger.Error("谱面文件初始化失败");
                }

                return initialized;
            }
            catch (OperationCanceledException)
            {
                logger.Warning("谱面文件初始化被取消");
                return false;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"谱面文件初始化异常: {ex.Message}");
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
    }
}
