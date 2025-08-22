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
    /// �����ѹ <see cref="ZipFile"/> �����н��� <see cref="Newtonsoft.Json"/> �ļ�
    /// </summary>
    public static class JsonManager
    {
        public static readonly string ZipPath = Application.persistentDataPath;

        /// <summary>
        /// ���� <see cref=" JsonConvert"/> �⽫��������л� <paramref name="Object"/> ����ѹ�� .json �ļ��洢�� <see cref="Application.persistentDataPath"/>���ļ���Ϊ <paramref name="ZipFileName"/>
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
        /// ��ʵ��� <paramref name="JsonName"/> ֻ��Ϊ���û������������Ϊ <see cref="LoadJsonFromZip{T}(string, ref T)"/> ��Ҫ���� <paramref name="JsonName"/>
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
                Debug.LogWarning($"����ʱ��⵽ {ZipFileName} �ļ��е� {JsonName} �﷨�ṹ����");
                return false;
            }
            catch (JsonSerializationException)
            {
                Debug.LogWarning(
                    $"����ʱ��⵽ {ZipFileName} �ļ��е� {JsonName} ��������ת������"
                );
                return false;
            }
            catch (Exception)
            {
                Debug.LogWarning($"����ʱ��⵽ {ZipFileName} �ļ��е� {JsonName} ��������");
                return false;
            }
        }

        /// <summary>
        /// ���� <see cref=" JsonConvert"/> ���λ�� <see cref="ZipPath"/> Ŀ¼����Ϊ <paramref name="ZipFileName"/> �� <see cref="ZipFile"/> �ж�ȡ��������л�Ϊ <paramref name="RefObject"/> ��json�ļ�
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
        /// ���� <see cref="JsonConvert"/> ��� <see cref="ZipPath"/> Ŀ¼���ļ���Ϊ <paramref name="ZipFileName"/> �� Zip �ļ��г��Զ�ȡ <typeparamref name="T"/> ���͵Ŀ����л� <paramref name="Object"/>
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
                Debug.LogWarning($"��ȡʱ��⵽ {ZipFileName} �ļ��е� {JsonName} �﷨�ṹ����");
                RefObject = Default;
                return false;
            }
            catch (JsonSerializationException)
            {
                Debug.LogWarning(
                    $"��ȡʱ��⵽ {ZipFileName} �ļ��е� {JsonName} ��������ת������"
                );
                RefObject = Default;
                return false;
            }
            catch (Exception)
            {
                Debug.LogWarning($"��ȡʱ��⵽ {ZipFileName} �ļ��е� {JsonName} ��������");
                RefObject = Default;
                return false;
            }
        }
    }
}
