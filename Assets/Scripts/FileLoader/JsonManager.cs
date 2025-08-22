using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
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
        /// 调用 <see cref=" JsonConvert"/> 库将任意可序列化 <paramref name="Object"/> 的已压缩 .json 文件存储到 <see cref="Application.persistentDataPath"/>。文件名为 <paramref name="ZipFileName"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Object"></param>
        /// <param name="ZipFileName"></param>
        /// <returns></returns>
        public static bool TrySaveJsonToZip<T>(string ZipFileName, T Object)
        {
            return TrySaveJsonToZip<T>(ZipFileName, Object, "Default.json");
        }

        /// <summary>
        /// 其实这个 <paramref name="JsonName"/> 只是为了用户看着舒服，因为 <see cref="LoadJsonFromZip{T}(string, ref T)"/> 不要求传入 <paramref name="JsonName"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Object"></param>
        /// <param name="ZipFileName"></param>
        /// <param name="JsonName"></param>
        /// <returns></returns>
        public static bool TrySaveJsonToZip<T>(string ZipFileName, T Object, string JsonName)
        {
            var Settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            string Json = JsonConvert.SerializeObject(Object, Settings);
            byte[] Byte = Encoding.Unicode.GetBytes(Json);
            byte[] Buffer = new byte[2048];
            try
            {
                var FS = File.Create(Path.Join(ZipPath, ZipFileName));
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
                Debug.LogWarning($"保存时检测到 {ZipFileName} 文件中的 {JsonName} 语法结构错误。");
                return false;
            }
            catch (JsonSerializationException)
            {
                Debug.LogWarning(
                    $"保存时检测到 {ZipFileName} 文件中的 {JsonName} 包含类型转化错误。"
                );
                return false;
            }
            catch (Exception)
            {
                Debug.LogWarning($"保存时检测到 {ZipFileName} 文件中的 {JsonName} 解析错误。");
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
            return TryLoadJsonFromZip<T>(ZipFileName, out RefObject, "Default.json", Default);
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

                    var Settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                    };

                    RefObject = JsonConvert.DeserializeObject<T>(Json, Settings);
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
