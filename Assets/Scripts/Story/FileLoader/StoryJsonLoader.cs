using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using ECS;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JsonLoader
{
    /// <summary>
    /// 负责解压 <see cref="ZipFile"/> 并从中解析 <see cref="Newtonsoft.Json"/> 文件
    /// </summary>
    public static class StoryJsonManager
    {
        public static readonly string ZipPath = Application.persistentDataPath;

        #region Initialization Methods

        /// <summary>
        /// 确保故事文件已初始化
        /// </summary>
        public static async UniTask<bool> EnsureStoryFilesInitializedAsync(
            bool forceReinitialize = false,
            CancellationToken cancellationToken = default
        )
        {
            using (var logger = new AsyncLogger(nameof(StoryJsonManager), true))
            {
                try
                {
                    logger.Info("开始初始化故事文件...");

                    // 首先确保基础文件系统已初始化
                    bool baseInitialized = await BaseJsonLoader.EnsureInitializedAsync(
                        forceReinitialize,
                        cancellationToken
                    );
                    if (!baseInitialized)
                    {
                        logger.Error("基础文件系统初始化失败");
                        return false;
                    }

                    logger.Info("故事文件初始化完成");
                    return true;
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
            }
        }

        #endregion

        #region Enhanced Load Methods with Initialization

        /// <summary>
        /// 带自动初始化的异步加载实体列表
        /// </summary>
        public static async UniTask<(
            bool success,
            List<T> entities
        )> TryLoadEntityListWithInitializationAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
            where T : Entity
        {
            // 确保故事文件已初始化
            bool initialized = await EnsureStoryFilesInitializedAsync(false, cancellationToken);
            if (!initialized)
            {
                if (output)
                {
                    LogManager.Error("故事文件初始化失败，无法加载文件", nameof(StoryJsonManager));
                }
                return (false, new List<T>());
            }

            // 执行加载
            return await TryLoadEntitiyListAsync<T>(fileName, output, cancellationToken);
        }

        /// <summary>
        /// 带自动初始化的同步加载实体列表
        /// </summary>
        public static bool TryLoadEntitiesWithInitialization<T>(
            string fileName,
            out List<T> entities,
            bool output = true
        )
            where T : Entity
        {
            entities = new List<T>();

            // 同步检查初始化状态（有限制）
            if (!FileInitializationManager.IsInitialized())
            {
                if (output)
                {
                    LogManager.Error(
                        "文件未初始化，请先调用 EnsureStoryFilesInitializedAsync",
                        nameof(StoryJsonManager)
                    );
                }
                return false;
            }

            // 执行加载
            return TryLoadEntities<T>(fileName, out entities, output);
        }

        /// <summary>
        /// 带自动初始化的 ZIP 加载方法
        /// </summary>
        public static async UniTask<(bool success, T result)> TryLoadZipWithInitializationAsync<T>(
            string zipFileName,
            string jsonName = BaseJsonLoader.DEFAULT_JSON_FILE_NAME,
            T defaultValue = default,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // 确保故事文件已初始化
            bool initialized = await EnsureStoryFilesInitializedAsync(false, cancellationToken);
            if (!initialized)
            {
                if (output)
                {
                    LogManager.Error("故事文件初始化失败，无法加载文件", nameof(StoryJsonManager));
                }
                return (false, defaultValue);
            }

            // 执行加载
            bool success = TryLoadJsonFromZip(
                zipFileName,
                out T result,
                jsonName,
                defaultValue,
                output
            );
            return (success, result);
        }

        #endregion

        #region Save Methods

        /// <summary>
        /// 异步保存对象到 ZIP 文件
        /// </summary>
        public static async UniTask<bool> TrySaveJsonToZipAsync<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings,
            string jsonName = BaseJsonLoader.DEFAULT_JSON_FILE_NAME,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            return await BaseJsonLoader.TrySaveJsonToZipAsync(
                zipFileName,
                serializeObject,
                settings,
                jsonName,
                output,
                cancellationToken
            );
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
            return await BaseJsonLoader.TrySaveJsonToZipAsync(
                zipFileName,
                serializeObject,
                settings,
                BaseJsonLoader.DEFAULT_JSON_FILE_NAME,
                output,
                cancellationToken
            );
        }

        public static bool TrySaveToZip(string zipFileName, string jsonContent, bool output = true)
        {
            return BaseJsonLoader.TrySaveToZip(zipFileName, jsonContent, output);
        }

        public static bool TrySaveJsonToZip<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings
        )
        {
            return TrySaveJsonToZip(
                zipFileName,
                serializeObject,
                settings,
                BaseJsonLoader.DEFAULT_JSON_FILE_NAME
            );
        }

        public static bool TrySaveJsonToZip<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings,
            string JsonName
        )
        {
            return BaseJsonLoader.TrySaveJsonToZip(
                zipFileName,
                serializeObject,
                settings,
                JsonName
            );
        }

        #endregion

        #region Load Methods

        /// <summary>
        /// 异步加载 JSON 文件并反序列化为实体列表
        /// </summary>
        public static async UniTask<(bool success, List<T> entities)> TryLoadEntitiyListAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
            where T : Entity
        {
            using (var logger = new AsyncLogger(nameof(StoryJsonManager), output))
            {
                logger.Info($"开始异步加载实体列表 [{fileName}]");

                List<T> entities = new List<T>();
                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
                    return (false, entities);
                }

                try
                {
                    // 使用 BaseJsonLoader 的异步方法加载 List<T>
                    var result = await BaseJsonLoader.TryLoadObjectFromJsonAsync<List<T>>(
                        fileName,
                        output,
                        cancellationToken
                    );

                    if (result.success)
                    {
                        entities = result.result ?? new List<T>();
                        logger.Info($"成功异步加载 {entities.Count} 个实体");
                    }
                    else
                    {
                        logger.Error("异步加载失败");
                    }

                    return (result.success, entities);
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("实体列表加载被取消");
                    return (false, entities);
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    return (false, entities);
                }
            }
        }

        /// <summary>
        /// 同步加载实体列表
        /// </summary>
        public static bool TryLoadEntities<T>(
            string fileName,
            out List<T> entities,
            bool output = true
        )
            where T : Entity
        {
            using (var logger = new AsyncLogger(nameof(StoryJsonManager), output))
            {
                logger.Info($"开始同步加载实体列表 [{fileName}]");

                entities = new List<T>();
                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
                    return false;
                }

                try
                {
                    // 使用 BaseJsonLoader 的同步方法加载 List<T>
                    bool success = BaseJsonLoader.TryLoadObjectFromJson(
                        fileName,
                        out List<T> result,
                        output
                    );

                    if (success && result != null)
                    {
                        entities = result;
                        logger.Info($"成功加载 {entities.Count} 个实体");
                        return true;
                    }
                    else
                    {
                        logger.Error("加载失败");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                    return false;
                }
            }
        }

        public static bool TryLoadJsonFromZip<T>(
            string ZipFileName,
            out T RefObject,
            T Default = default
        )
        {
            return TryLoadJsonFromZip(
                ZipFileName,
                out RefObject,
                BaseJsonLoader.DEFAULT_JSON_FILE_NAME,
                Default
            );
        }

        public static bool TryLoadJsonFromZip<T>(
            string zipFileName,
            out T result,
            string jsonName = BaseJsonLoader.DEFAULT_JSON_FILE_NAME,
            T defaultValue = default,
            bool output = true
        )
        {
            return BaseJsonLoader.TryLoadJsonFromZip(
                zipFileName,
                out result,
                jsonName,
                defaultValue,
                output
            );
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 检查 Json 所解包出来的 <see cref="List{Entity}"/> 是否正常
        /// </summary>
        public static bool ValidateJsonStructure(string json, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var jToken = JToken.Parse(json);
                if (jToken.Type != JTokenType.Array)
                {
                    errorMessage = "JSON 不是数组";
                    return false;
                }
                return true;
            }
            catch (JsonReaderException ex)
            {
                errorMessage = $"JSON 解析错误: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"未知错误: {ex.Message}";
                return false;
            }
        }

        #endregion
    }
}
