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
            string Json = JsonConvert.SerializeObject(serializeObject, settings);
            byte[] Byte = Encoding.Unicode.GetBytes(Json);
            byte[] Buffer = new byte[2048];
            try
            {
                var FS = File.Create(Path.Join(ZipPath, zipFileName));
                using (ZipOutputStream ZS = new ZipOutputStream(FS))
                {
                    ZS.SetLevel(5);
                    ZipEntry Path = new ZipEntry(JsonName)
                    {
                        IsUnicodeText = true,
                        DateTime = DateTime.Now,
                    };
                    ZS.PutNextEntry(Path);
                    using (MemoryStream MS = new MemoryStream(Byte))
                    {
                        StreamUtils.Copy(MS, ZS, Buffer);
                    }
                    ZS.CloseEntry();
                    ZS.IsStreamOwner = false;
                    ZS.Finish();
                    ZS.Close();
                }
                FS.Close();
                Debug.Log("Succesfully Saved!");
                return true;
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

        // 添加详细的 JSON 调试方法
        public static bool TryLoadJsonWithDebug(string fileName, out List<Entity> entities)
        {
            entities = new List<Entity>();

            var filePath = Path.Join(ZipPath, fileName);

            try
            {
                using (
                    ZipInputStream ZS = new ZipInputStream(
                        File.OpenRead(Path.Join(ZipPath, fileName))
                    )
                )
                {
                    ZS.GetNextEntry();
                    byte[] ZipBuffer = new byte[ZS.Length];
                    ZS.Read(ZipBuffer, 0, ZipBuffer.Length);
                    string Json = Encoding.Unicode.GetString(ZipBuffer);

                    if (!File.Exists(filePath))
                    {
                        LogFile.Error($"文件不存在: {filePath}", "JsonSerializer");
                        return false;
                    }

                    string json = Json;

                    LogFile.Info($"读取的 JSON 内容: {json}", "JsonSerializer");

                    // 尝试解析 JSON 结构
                    try
                    {
                        // 首先尝试解析为 JToken 来检查 JSON 结构
                        var jToken = JToken.Parse(json);
                        LogFile.Info($"JSON 类型: {jToken.Type}", "JsonSerializer");

                        if (jToken.Type == JTokenType.Array)
                        {
                            LogFile.Info(
                                $"JSON 是数组，包含 {((JArray)jToken).Count} 个元素",
                                "JsonSerializer"
                            );

                            // 检查数组中的第一个元素
                            if (((JArray)jToken).Count > 0)
                            {
                                var firstElement = ((JArray)jToken)[0];
                                LogFile.Info(
                                    $"第一个元素类型: {firstElement.Type}",
                                    "JsonSerializer"
                                );

                                if (firstElement.Type == JTokenType.Object)
                                {
                                    var firstObject = (JObject)firstElement;
                                    LogFile.Info(
                                        $"第一个对象包含的键: {string.Join(", ", firstObject.Properties().Select(p => p.Name))}",
                                        "JsonSerializer"
                                    );
                                }
                            }
                        }
                        else if (jToken.Type == JTokenType.Object)
                        {
                            LogFile.Info($"JSON 是对象", "JsonSerializer");
                            var jObject = (JObject)jToken;
                            LogFile.Info(
                                $"对象包含的键: {string.Join(", ", jObject.Properties().Select(p => p.Name))}",
                                "JsonSerializer"
                            );
                        }
                    }
                    catch (Exception parseEx)
                    {
                        LogFile.Exception(parseEx, "JsonSerializer");
                        LogFile.Error($"JSON 解析失败: {parseEx.Message}", "JsonSerializer");
                        return false;
                    }

                    // 尝试反序列化为 Entity 列表
                    try
                    {
                        entities = JsonConvert.DeserializeObject<List<Entity>>(json);
                        LogFile.Info($"成功反序列化 {entities.Count} 个实体", "JsonSerializer");
                        return true;
                    }
                    catch (Exception deserializeEx)
                    {
                        LogFile.Exception(deserializeEx, "JsonSerializer");
                        LogFile.Error($"反序列化失败: {deserializeEx.Message}", "JsonSerializer");

                        // 尝试替代方法：先反序列化为 JArray，然后手动转换为 Entity
                        try
                        {
                            var jArray = JArray.Parse(json);
                            entities = new List<Entity>();

                            foreach (var item in jArray)
                            {
                                try
                                {
                                    var entity = item.ToObject<Entity>();
                                    if (entity != null)
                                    {
                                        entities.Add(entity);
                                    }
                                }
                                catch (Exception itemEx)
                                {
                                    LogFile.Exception(itemEx, "JsonSerializer");
                                    LogFile.Error(
                                        $"反序列化单个实体失败: {itemEx.Message}",
                                        "JsonSerializer"
                                    );
                                }
                            }

                            LogFile.Info(
                                $"通过替代方法成功反序列化 {entities.Count} 个实体",
                                "JsonSerializer"
                            );
                            return entities.Count > 0;
                        }
                        catch (Exception fallbackEx)
                        {
                            LogFile.Exception(fallbackEx, "JsonSerializer");
                            LogFile.Error(
                                $"替代方法也失败: {fallbackEx.Message}",
                                "JsonSerializer"
                            );
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogFile.Exception(ex, "JsonSerializer");
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
        /// <param name="ZipFileName"></param>
        /// <param name="RefObject"></param>
        /// <returns></returns>
        public static bool TryLoadJsonFromZip<T>(
            string ZipFileName,
            out T RefObject,
            string JsonName,
            T Default
        )
        {
            try
            {
                using (
                    ZipInputStream ZS = new ZipInputStream(
                        File.OpenRead(Path.Join(ZipPath, ZipFileName))
                    )
                )
                {
                    ZS.GetNextEntry();
                    byte[] ZipBuffer = new byte[ZS.Length];
                    ZS.Read(ZipBuffer, 0, ZipBuffer.Length);
                    string Json = Encoding.Unicode.GetString(ZipBuffer);

                    RefObject = JsonConvert.DeserializeObject<T>(Json);
                    ZS.CloseEntry();
                    ZS.IsStreamOwner = false;
                    ZS.Close();
                    Debug.Log("Succesfully Loaded!");
                    return true;
                }
            }
            catch (JsonReaderException)
            {
                Debug.LogWarning($"读取时检测到 {ZipFileName} 文件中的 {JsonName} 语法结构错误。");
                RefObject = Default;
                return false;
            }
            catch (JsonSerializationException)
            {
                Debug.LogWarning(
                    $"读取时检测到 {ZipFileName} 文件中的 {JsonName} 包含类型转化错误。"
                );
                RefObject = Default;
                return false;
            }
            catch (Exception)
            {
                Debug.LogWarning($"读取时检测到 {ZipFileName} 文件中的 {JsonName} 解析错误。");
                RefObject = Default;
                return false;
            }
        }
    }
}
