using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ECS;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JsonLoader
{
    /// <summary>
    /// 负责解压 <see cref="ZipFile"/> 并从中解析 <see cref="Newtonsoft.Json"/> 文件
    /// </summary>
    public static class JsonManager
    {
        public static readonly string ZipPath = Application.persistentDataPath;

        public static bool TrySaveToZip(string zipFileName, string jsonContent)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(jsonContent);
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
                    zipStream.SetLevel(5);

                    var entry = new ZipEntry(zipFileName)
                    {
                        IsUnicodeText = true,
                        DateTime = DateTime.Now,
                    };

                    zipStream.PutNextEntry(entry);

                    using (var memoryStream = new MemoryStream(buffer))
                    {
                        StreamUtils.Copy(memoryStream, zipStream, new byte[2048]);
                    }

                    zipStream.CloseEntry();
                    // 不需要手动调用 Close()，using 块会自动处理
                }

                Debug.Log("Successfully Saved!");
                return true;
            }
            catch (Exception ex)
            {
                LogFile.Exception(ex, "JsonManager");
                Debug.LogError($"保存 Zip 文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 调用 <see cref=" JsonConvert"/> 库将任意可序列化 <paramref name="serializeObject"/> 的已压缩 .json 文件存储到 <see cref="Application.persistentDataPath"/>。文件名为 <paramref name="zipFileName"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializeObject"></param>
        /// <param name="zipFileName"></param>
        /// <returns></returns>
        public static bool TrySaveJsonToZip<T>(
            string zipFileName,
            T serializeObject,
            JsonSerializerSettings settings
        )
        {
            return TrySaveJsonToZip<T>(zipFileName, serializeObject, settings, "Default.json");
        }

        /// <summary>
        /// 其实这个 <paramref name="JsonName"/> 只是为了用户看着舒服，因为 <see cref="LoadJsonFromZip{T}(string, ref T)"/> 不要求传入 <paramref name="JsonName"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializeObject"></param>
        /// <param name="zipFileName"></param>
        /// <param name="JsonName"></param>
        /// <returns></returns>
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

        public static bool TryLoadJsonWithDebug<T>(string fileName, out List<T> entities)
        {
            entities = new List<T>();
            string fullPath = Path.Combine(ZipPath, fileName);

            if (!File.Exists(fullPath))
            {
                LogFile.Error($"文件不存在: {fullPath}", "JsonManager");
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
                    ZipEntry entry = zipStream.GetNextEntry();
                    if (entry == null)
                    {
                        LogFile.Error($"ZIP 文件中没有条目: {fullPath}", "JsonManager");
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

                        string json = Encoding.Unicode.GetString(memoryStream.ToArray());
                        LogFile.Info($"读取的 JSON 内容: {json}", "JsonManager");

                        // 尝试解析 JSON 结构
                        try
                        {
                            var jToken = JToken.Parse(json);
                            LogFile.Info($"JSON 类型: {jToken.Type}", "JsonManager");

                            if (jToken.Type == JTokenType.Array)
                            {
                                var jArray = (JArray)jToken;
                                LogFile.Info(
                                    $"JSON 是数组，包含 {jArray.Count} 个元素",
                                    "JsonManager"
                                );

                                if (jArray.Count > 0)
                                {
                                    var firstElement = jArray[0];
                                    LogFile.Info(
                                        $"第一个元素类型: {firstElement.Type}",
                                        "JsonManager"
                                    );

                                    if (firstElement.Type == JTokenType.Object)
                                    {
                                        var firstObject = (JObject)firstElement;
                                        var properties = firstObject
                                            .Properties()
                                            .Select(p => p.Name)
                                            .ToList();
                                        LogFile.Info(
                                            $"第一个对象包含的键: {string.Join(", ", properties)}",
                                            "JsonManager"
                                        );

                                        // 检查是否包含必要的属性
                                        if (!properties.Contains("Id"))
                                        {
                                            LogFile.Warning(
                                                "第一个对象缺少 'Id' 属性",
                                                "JsonManager"
                                            );
                                        }

                                        if (!properties.Contains("_components"))
                                        {
                                            LogFile.Warning(
                                                "第一个对象缺少 '_components' 属性",
                                                "JsonManager"
                                            );
                                        }
                                    }
                                }
                            }
                            else
                            {
                                LogFile.Error($"JSON 不是数组，而是 {jToken.Type}", "JsonManager");
                                return false;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            LogFile.Exception(parseEx, "JsonManager");
                            LogFile.Error($"JSON 解析失败: {parseEx.Message}", "JsonManager");
                        }

                        // 尝试反序列化
                        try
                        {
                            var settings = new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto,
                                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore,
                                Error = (sender, args) =>
                                {
                                    LogFile.Error(
                                        $"反序列化错误: {args.ErrorContext.Error.Message}",
                                        "JsonManager"
                                    );
                                    args.ErrorContext.Handled = true;
                                },
                            };

                            entities = JsonConvert.DeserializeObject<List<T>>(json, settings);

                            if (entities == null)
                            {
                                LogFile.Error("反序列化返回 null", "JsonManager");
                                return false;
                            }

                            LogFile.Info($"成功反序列化 {entities.Count} 个实体", "JsonManager");

                            // 检查实体是否有效
                            if (entities.Count > 0)
                            {
                                var firstEntity = entities[0];
                                var entityType = firstEntity.GetType();
                                LogFile.Info(
                                    $"第一个实体类型: {entityType.FullName}",
                                    "JsonManager"
                                );

                                // 尝试获取 Id 属性（如果存在）
                                var idProperty = entityType.GetProperty("Id");
                                if (idProperty != null)
                                {
                                    var idValue = idProperty.GetValue(firstEntity);
                                    LogFile.Info($"第一个实体的 Id: {idValue}", "JsonManager");
                                }
                            }

                            return true;
                        }
                        catch (Exception deserializeEx)
                        {
                            LogFile.Exception(deserializeEx, "JsonManager");
                            LogFile.Error($"反序列化失败: {deserializeEx.Message}", "JsonManager");

                            // 尝试替代方法 - 手动解析数组
                            try
                            {
                                var jArray = JArray.Parse(json);
                                entities = new List<T>();

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
                                        LogFile.Exception(itemEx, "JsonManager");
                                        LogFile.Error(
                                            $"无法反序列化数组元素: {itemEx.Message}",
                                            "JsonManager"
                                        );

                                        // 记录有问题的元素
                                        LogFile.Error(
                                            $"问题元素: {item.ToString()}",
                                            "JsonManager"
                                        );
                                    }
                                }

                                LogFile.Info(
                                    $"通过替代方法成功反序列化 {entities.Count} 个实体",
                                    "JsonManager"
                                );
                                return entities.Count > 0;
                            }
                            catch (Exception fallbackEx)
                            {
                                LogFile.Exception(fallbackEx, "JsonManager");
                                LogFile.Error(
                                    $"替代方法也失败: {fallbackEx.Message}",
                                    "JsonManager"
                                );

                                // 尝试使用不同的编码
                                try
                                {
                                    string jsonUtf8 = Encoding.UTF8.GetString(
                                        memoryStream.ToArray()
                                    );
                                    LogFile.Info($"尝试使用 UTF-8 编码: {jsonUtf8}", "JsonManager");

                                    entities = JsonConvert.DeserializeObject<List<T>>(jsonUtf8);
                                    LogFile.Info(
                                        $"使用 UTF-8 编码成功反序列化 {entities.Count} 个实体",
                                        "JsonManager"
                                    );
                                    return entities.Count > 0;
                                }
                                catch (Exception encodingEx)
                                {
                                    LogFile.Exception(encodingEx, "JsonManager");
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.Exception(ex, "JsonManager");
                return false;
            }
        }

        /// <summary>
        /// 调用 <see cref=" JsonConvert"/> 库从位于 <see cref="ZipPath"/> 目录下名为 <paramref name="ZipFileName"/> 的 <see cref="ZipFile"/> 中读取任意可序列化为 <paramref name="RefObject"/> 的json文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="RefObject"></param>
        /// <param name="ZipFileName"></param>
        /// <returns></returns>
        public static bool TryLoadJsonFromZip<T>(
            string ZipFileName,
            out T RefObject,
            T Default = default
        )
        {
            return TryLoadJsonFromZip(ZipFileName, out RefObject, "Default.json", Default);
        }

        /// <summary>
        /// 调用 <see cref="JsonConvert"/> 库从 <see cref="ZipPath"/> 目录下文件名为 <paramref name="ZipFileName"/> 的 Zip 文件中尝试读取 <typeparamref name="T"/> 类型的可序列化 <paramref name="Object"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="zipFileName"></param>
        /// <param name="result"></param>
        /// <param name="jsonName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool TryLoadJsonFromZip<T>(
            string zipFileName,
            out T result,
            string jsonName = "Default.json",
            T defaultValue = default
        )
        {
            result = defaultValue;
            string fullPath = Path.Combine(ZipPath, zipFileName);

            if (!File.Exists(fullPath))
            {
                LogFile.Error($"文件不存在: {fullPath}", "JsonManager");
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

                                LogFile.Info(
                                    $"成功从 {zipFileName} 加载 {jsonName}",
                                    "JsonManager"
                                );
                                return true;
                            }
                        }
                    }

                    LogFile.Warning($"在 {zipFileName} 中未找到 {jsonName}", "JsonManager");
                    return false;
                }
            }
            catch (IOException ioEx)
            {
                LogFile.Exception(ioEx, "JsonManager");
                LogFile.Error($"文件访问错误: {ioEx.Message}", "JsonManager");
                return false;
            }
            catch (JsonException jsonEx)
            {
                LogFile.Exception(jsonEx, "JsonManager");
                LogFile.Error($"JSON 解析错误: {jsonEx.Message}", "JsonManager");
                return false;
            }
            catch (Exception ex)
            {
                LogFile.Exception(ex, "JsonManager");
                LogFile.Error($"未知错误: {ex.Message}", "JsonManager");
                return false;
            }
        }
    }
}
