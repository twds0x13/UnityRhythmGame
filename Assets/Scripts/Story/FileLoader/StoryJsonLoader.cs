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
        /// 异步加载 JSON 文件并反序列化为实体列表，使用 <see cref="AsyncLogger"/> 记录日志
        /// </summary>
        public static async UniTask<(bool success, List<T> entities)> TryLoadEntitiyListAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
            where T : Entity // 添加约束，确保 T 是 Entity 类型
        {
            // 创建异步日志记录器
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

                    // 记录结果
                    if (result.success)
                    {
                        entities = result.result ?? new List<T>();
                        logger.Info($"成功异步加载 {entities.Count} 个实体");

                        // 验证实体
                        if (entities.Count > 0 && !ValidateEntities(entities, logger))
                        {
                            logger.Warning("部分实体验证失败");
                        }
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
                logger.Info($"======开始同步加载实体列表 [{fileName}] ======");

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
                        false
                    );

                    if (success && result != null)
                    {
                        entities = result;
                        logger.Info($"成功加载 {entities.Count} 个实体");

                        // 验证实体
                        if (entities.Count > 0 && !ValidateEntities(entities, logger))
                        {
                            logger.Warning("部分实体验证失败");
                        }

                        logger.Info($"======完成加载实体列表 [{fileName}] ======");
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

        /// <summary>
        /// 异步加载 JSON 文件并反序列化，使用 <see cref="AsyncLogger"/> 记录日志
        /// </summary>
        public static async UniTask<(bool success, List<T> entities)> TryLoadJsonDebugAsync<T>(
            string fileName,
            bool output = true,
            CancellationToken cancellationToken = default
        )
        {
            // 创建异步日志记录器
            using (var logger = new AsyncLogger(nameof(StoryJsonManager), output))
            {
                logger.Info($"开始异步加载文件 [{fileName}]");

                List<T> entities = new List<T>();
                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
                    return (false, entities);
                }

                try
                {
                    // 使用 UniTask.RunOnThreadPool 在后台线程执行同步操作
                    var result = await UniTask.RunOnThreadPool(
                        () =>
                        {
                            bool success = TryLoadJsonDebug(
                                fileName,
                                out List<T> resultEntities,
                                true
                            );
                            return (success, resultEntities);
                        },
                        cancellationToken: cancellationToken
                    );

                    // 记录结果
                    if (result.success)
                    {
                        logger.Info($"成功异步加载 {result.resultEntities.Count} 个实体");
                    }
                    else
                    {
                        logger.Error("异步加载失败");
                    }

                    return result;
                }
                catch (OperationCanceledException)
                {
                    logger.Warning("JSON 加载被取消");
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
        /// 调用 <see cref=" JsonConvert"/> 库从位于 <see cref="ZipPath"/> 目录下的 <paramref name="fileName"/> 文件反序列化出 <see cref="List{T}"/> 类型的列表。
        /// 添加了 <see cref="async"/> 版本 <see cref="TryLoadJsonDebugAsync{T}(string, bool, CancellationToken)"/>。
        /// </summary>
        public static bool TryLoadJsonDebug<T>(
            string fileName,
            out List<T> entities,
            bool output = true
        )
        {
            using (var logger = new AsyncLogger(nameof(StoryJsonManager), output))
            {
                logger.Info($"======Json.开始加载文件 [{fileName}] ======");

                entities = new List<T>();
                string fullPath = Path.Combine(ZipPath, fileName);

                if (!File.Exists(fullPath))
                {
                    logger.Error($"文件不存在: {fullPath}");
                    return false;
                }

                try
                {
                    // 读取 ZIP 文件内容
                    if (!BaseJsonLoader.TryReadZipContent(fullPath, out string json, logger))
                    {
                        return false;
                    }

                    logger.Info($"读取的 JSON 内容: {json}");

                    // 验证 JSON 结构
                    string errorMessage;
                    if (!ValidateJsonStructure(json, out errorMessage))
                    {
                        logger.Error($"JSON 结构验证失败: {errorMessage}");
                    }

                    // 尝试反序列化
                    if (TryDeserializeEntities(json, out entities, logger))
                    {
                        logger.Info($"成功反序列化 {entities.Count} 个实体");
                        LogFirstEntityInfo(entities, logger);
                        logger.Info($"======Json.完成加载文件 [{fileName}] ======");
                        return true;
                    }

                    // 尝试替代方法
                    if (TryFallbackDeserialization(json, out entities, logger))
                    {
                        logger.Info($"通过替代方法成功反序列化 {entities.Count} 个实体");
                        logger.Info($"======Json.完成加载文件 [{fileName}] ======");
                        return true;
                    }

                    // 尝试使用 UTF-8 编码
                    if (TryUtf8Deserialization(fullPath, out entities, logger))
                    {
                        logger.Info($"使用 UTF-8 编码成功反序列化 {entities.Count} 个实体");
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
        /// 尝试反序列化实体列表
        /// </summary>
        private static bool TryDeserializeEntities<T>(
            string json,
            out List<T> entities,
            AsyncLogger logger
        )
        {
            entities = new List<T>();

            try
            {
                var deserializeSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    Error = (sender, args) =>
                    {
                        logger.Error($"反序列化错误: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                };

                entities = JsonConvert.DeserializeObject<List<T>>(json, deserializeSettings);

                if (entities == null)
                {
                    logger.Error("反序列化返回 null");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"反序列化失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试替代方法反序列化
        /// </summary>
        private static bool TryFallbackDeserialization<T>(
            string json,
            out List<T> entities,
            AsyncLogger logger
        )
        {
            entities = new List<T>();

            try
            {
                var jArray = JArray.Parse(json);

                foreach (var item in jArray)
                {
                    try
                    {
                        var entity = item.ToObject<T>();
                        if (entity != null)
                        {
                            entities.Add(entity);
                        }
                    }
                    catch (Exception itemEx)
                    {
                        logger.Exception(itemEx);
                        logger.Error($"无法反序列化数组元素: {itemEx.Message}");
                        logger.Error($"问题元素: {item.ToString()}");
                    }
                }

                return entities.Count > 0;
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                logger.Error($"替代方法也失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试使用 UTF-8 编码反序列化
        /// </summary>
        private static bool TryUtf8Deserialization<T>(
            string fullPath,
            out List<T> entities,
            AsyncLogger logger
        )
        {
            entities = new List<T>();

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

                        string jsonUtf8 = Encoding.UTF8.GetString(memoryStream.ToArray());
                        logger.Info($"尝试使用 UTF-8 编码: {jsonUtf8}");

                        entities = JsonConvert.DeserializeObject<List<T>>(jsonUtf8);
                        return entities != null && entities.Count > 0;
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
        /// 记录第一个实体的信息
        /// </summary>
        private static void LogFirstEntityInfo<T>(List<T> entities, AsyncLogger logger)
        {
            if (entities.Count > 0)
            {
                var firstEntity = entities[0];
                var entityType = firstEntity.GetType();
                logger.Info($"第一个实体类型: {entityType.FullName}");

                // 尝试获取 Id 属性（如果存在）
                var idProperty = entityType.GetProperty("Id");
                if (idProperty != null)
                {
                    var idValue = idProperty.GetValue(firstEntity);
                    logger.Info($"第一个实体的 Id: {idValue}");
                }
            }
        }

        /// <summary>
        /// 验证实体列表的有效性
        /// </summary>
        private static bool ValidateEntities<T>(List<T> entities, AsyncLogger logger)
            where T : Entity
        {
            bool allValid = true;

            foreach (var entity in entities)
            {
                if (string.IsNullOrEmpty(entity.Id.ToString()))
                {
                    logger.Warning($"实体缺少 ID: {entity.GetType().Name}");
                    allValid = false;
                }

                // 添加更多验证逻辑...
            }

            return allValid;
        }

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
                var jArray = (JArray)jToken;
                if (jArray.Count == 0)
                {
                    errorMessage = "JSON 数组为空";
                    return false;
                }
                foreach (var item in jArray)
                {
                    if (item.Type != JTokenType.Object)
                    {
                        errorMessage = "数组元素不是对象";
                        return false;
                    }
                    var jObject = (JObject)item;
                    var properties = jObject.Properties().Select(p => p.Name).ToList();
                    if (!properties.Contains("Id"))
                    {
                        errorMessage = "对象缺少 'Id' 属性";
                        return false;
                    }
                    if (!properties.Contains("_components"))
                    {
                        errorMessage = "对象缺少 '_components' 属性";
                        return false;
                    }
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

        /// <summary>
        /// 检查基本的 Json 解析过程是否成功
        /// </summary>
        public static bool ValidateDeserializeStructure<T>(
            string json,
            out List<T> entities,
            out string errorMessage
        )
        {
            entities = new List<T>();
            errorMessage = string.Empty;
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                };
                entities = JsonConvert.DeserializeObject<List<T>>(json, settings);
                if (entities == null)
                {
                    errorMessage += "反序列化返回 null\n";
                    return false;
                }
                return true;
            }
            catch (JsonException jsonEx)
            {
                errorMessage += $"JSON 解析错误: {jsonEx.Message}\n";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage += $"未知错误: {ex.Message}\n";
                return false;
            }
        }

        #endregion
    }
}
