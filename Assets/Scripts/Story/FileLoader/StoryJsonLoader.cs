using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using ECS;
using JsonLoader.JsonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonLoader
{
    /// <summary>
    /// 故事数据管理器 - 负责故事相关的JSON文件操作
    /// </summary>
    public sealed class StoryReader : BaseJsonLoader
    {
        #region 配置常量

        /// <summary>
        /// 默认故事配置
        /// </summary>
        private static LoadConfig DefaultConfig =>
            new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.Unicode,
                SearchSubdirectories = true,
                AutoInitialize = true,
            };

        #endregion

        #region 核心加载方法

        /// <summary>
        /// 加载故事实体列表
        /// </summary>
        public static (bool success, List<Entity> entities) LoadEntities(
            string fileName,
            LoadConfig config = null
        )
        {
            config ??= DefaultConfig;
            return LoadObject<List<Entity>>(fileName, config: config);
        }

        /// <summary>
        /// 异步加载故事实体列表
        /// </summary>
        public static async UniTask<List<Entity>> LoadEntitiesAsync(
            string fileName,
            LoadConfig config = null
        )
        {
            config ??= DefaultConfig;
            var (success, entities) = await LoadObjectAsync<List<Entity>>(fileName, config: config);
            return entities ?? new List<Entity>();
        }

        /// <summary>
        /// 加载故事数据对象
        /// </summary>
        public static (bool success, T storyData) LoadStory<T>(
            string zipFile,
            LoadConfig config = null
        )
        {
            config ??= DefaultConfig;
            return LoadObject<T>(zipFile, config: config);
        }

        /// <summary>
        /// 异步加载故事数据对象
        /// </summary>
        public static async UniTask<T> LoadStoryAsync<T>(string zipFile, LoadConfig config = null)
        {
            config ??= DefaultConfig;
            var (success, result) = await LoadObjectAsync<T>(zipFile, config: config);
            return result;
        }

        #endregion

        #region 保存方法

        /// <summary>
        /// 保存故事数据
        /// </summary>
        public static bool SaveStory<T>(string zipFile, T storyData, LoadConfig config = null)
        {
            config ??= DefaultConfig;
            return SaveObject(zipFile, storyData, config);
        }

        /// <summary>
        /// 异步保存故事数据
        /// </summary>
        public static async UniTask<bool> SaveStoryAsync<T>(
            string zipFile,
            T storyData,
            LoadConfig config = null
        )
        {
            config ??= DefaultConfig;
            return await SaveObjectAsync(zipFile, storyData, config);
        }

        /// <summary>
        /// 保存故事JSON内容
        /// </summary>
        public static bool SaveStoryJson(
            string zipFile,
            string jsonContent,
            LoadConfig config = null
        )
        {
            config ??= DefaultConfig;
            return SaveObject(zipFile, jsonContent, config);
        }

        #endregion

        #region 初始化相关

        /// <summary>
        /// 确保故事文件已初始化
        /// </summary>
        public static async UniTask<bool> EnsureInitializedAsync(LoadConfig config = null)
        {
            config ??= DefaultConfig;
            bool shouldDisposeLogger = config.Logger == null;
            var logger = config.Logger ?? new AsyncLogger(nameof(StoryReader), config.OutputLog);

            try
            {
                logger.Info("开始初始化故事文件...");

                // 使用基类的初始化方法
                bool initialized = await BaseJsonLoader.EnsureInitializedAsync(
                    config.CancellationToken,
                    logger
                );

                if (initialized)
                {
                    logger.Info("故事文件初始化完成");
                }
                else
                {
                    logger.Error("故事文件初始化失败");
                }

                return initialized;
            }
            catch (OperationCanceledException)
            {
                logger.Warning("故事文件初始化被取消");
                return false;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"故事文件初始化异常: {ex.Message}");
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

        #region 便捷方法

        /// <summary>
        /// 快速加载故事实体列表（使用默认配置）
        /// </summary>
        public static async UniTask<List<Entity>> LoadEntitiesQuickAsync(
            string fileName,
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            var config = new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.Unicode,
                AutoInitialize = true,
                CancellationToken = cancellationToken,
                Logger = logger,
            };

            return await LoadEntitiesAsync(fileName, config);
        }

        /// <summary>
        /// 快速加载故事数据（使用默认配置）
        /// </summary>
        public static async UniTask<T> LoadStoryQuickAsync<T>(
            string zipFile,
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            var config = new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.Unicode,
                AutoInitialize = true,
                CancellationToken = cancellationToken,
                Logger = logger,
            };

            return await LoadStoryAsync<T>(zipFile, config);
        }

        /// <summary>
        /// 快速保存故事数据（使用默认配置）
        /// </summary>
        public static async UniTask<bool> SaveStoryQuickAsync<T>(
            string zipFile,
            T storyData,
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            var config = new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.Unicode,
                CancellationToken = cancellationToken,
                Logger = logger,
            };

            return await SaveStoryAsync(zipFile, storyData, config);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 验证故事JSON结构
        /// </summary>
        public static bool ValidateStoryStructure(string json, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var jToken = JToken.Parse(json);
                if (jToken.Type != JTokenType.Array)
                {
                    errorMessage = "故事JSON应该是数组格式";
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
        /// 检查故事文件是否存在
        /// </summary>
        public static bool StoryExists(string zipFile)
        {
            string fullPath = Path.Combine(ZipPath, zipFile);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// 获取故事文件信息
        /// </summary>
        public static FileInfo GetStoryFileInfo(string zipFile)
        {
            string fullPath = Path.Combine(ZipPath, zipFile);
            return new FileInfo(fullPath);
        }

        /// <summary>
        /// 获取故事文件统计信息
        /// </summary>
        public static FileInfoStats GetStoryFileStats()
        {
            return FileInitializationManager.GetFileInfoStats();
        }

        #endregion

        #region 高级配置方法

        /// <summary>
        /// 创建自定义配置的加载器
        /// </summary>
        public static LoadConfig CreateConfig(
            bool outputLog = true,
            JsonEncoding encoding = JsonEncoding.Unicode,
            bool autoInitialize = true,
            bool searchSubdirectories = true,
            CancellationToken cancellationToken = default,
            AsyncLogger logger = null
        )
        {
            return new LoadConfig
            {
                OutputLog = outputLog,
                Encoding = encoding,
                AutoInitialize = autoInitialize,
                SearchSubdirectories = searchSubdirectories,
                CancellationToken = cancellationToken,
                Logger = logger,
            };
        }

        /// <summary>
        /// 创建静默配置（无日志输出）
        /// </summary>
        public static LoadConfig CreateSilentConfig(CancellationToken cancellationToken = default)
        {
            return new LoadConfig
            {
                OutputLog = false,
                Encoding = JsonEncoding.Unicode,
                AutoInitialize = true,
                SearchSubdirectories = true,
                CancellationToken = cancellationToken,
            };
        }

        /// <summary>
        /// 创建调试配置（详细日志输出）
        /// </summary>
        public static LoadConfig CreateDebugConfig(
            AsyncLogger logger = null,
            CancellationToken cancellationToken = default
        )
        {
            return new LoadConfig
            {
                OutputLog = true,
                Encoding = JsonEncoding.Unicode,
                AutoInitialize = true,
                SearchSubdirectories = true,
                CancellationToken = cancellationToken,
                Logger = logger,
            };
        }

        #endregion
    }
}
