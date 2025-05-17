using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using IUtils;
using R3;
using UnityEngine;

namespace GBehaviour
{
    /// <summary>
    /// 用来存储用户设置，构造函数内含有默认游戏设置
    /// </summary>
    public struct GameSettings
    {
        public float JudgementTimeOffset;

        public float DisplayTimeOffset;

        public GameSettings(float JudgementTimeOffset = 0f, float DisplayTimeOffset = 0f)
        {
            this.JudgementTimeOffset = JudgementTimeOffset;

            this.DisplayTimeOffset = DisplayTimeOffset;
        }
    }

    /// <summary>
    /// 用来管理游戏相关的行为，包括暂停，修改用户设置等等
    /// </summary>
    public class GameBehaviour : MonoBehaviour, IDev, ITime
    {
        private GameBehaviour() { }

        private static GameBehaviour GBinstance = null;

        public static GameBehaviour Inst
        {
            get { return GBinstance; }
        } // 用了单例模式

        private static GameSettings GameSettings;

        private string UserSettingsZipPath = Application.streamingAssetsPath; // Zip 文件默认存放在 StreamingAssets 文件夹内

        private static Timer MainTimer;

        private static Timer AbsTimer;

        public bool GamePaused { get; private set; }

        public void Awake()
        {
            if (GBinstance != null && GBinstance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                GBinstance = this;
            }

            DontDestroyOnLoad(this.gameObject);

            MainTimer = Timer.Register(
                duration: 1145141919810f,
                onComplete: null,
                useRealTime: false
            );

            AbsTimer = Timer.Register(
                duration: 1145141919810f,
                onComplete: null,
                useRealTime: true
            );
        }

        public void DevLog()
        {
            Debug.LogFormat(
                "Mono Behaviour Load Time Usage: {0} ms",
                (AbsTimer.GetTimeElapsed() - MainTimer.GetTimeElapsed()) * 1000
            );

            SaveGameSettings();
        }

        public float JudgeTime()
        {
            return MainTimer.GetTimeElapsed() + GameSettings.JudgementTimeOffset;
        }

        public float JudgeTimeMs()
        {
            return (MainTimer.GetTimeElapsed() + GameSettings.JudgementTimeOffset) * 1000;
        }

        public float AbsTimeMs()
        {
            return AbsTimer.GetTimeElapsed() * 1000; // 输出毫秒
        }

        public float AbsTime()
        {
            return AbsTimer.GetTimeElapsed();
        }

        public void Pause()
        {
            MainTimer.Pause();

            GamePaused = true;
        }

        public void Resume()
        {
            MainTimer.Resume();

            GamePaused = false;
        }

        public void SaveGameSettings()
        {
            string UserSettingsJson = JsonUtility.ToJson(GameSettings);
            CompressToZip(Encoding.Unicode.GetBytes(UserSettingsJson), "UserSettings.zip");
        }

        public void CompressToZip(byte[] Bytes, string ZipFileName)
        {
            byte[] Buffer = new byte[2048];
            using (
                ZipOutputStream ZS = new ZipOutputStream(
                    File.Create(Path.Join(UserSettingsZipPath, ZipFileName))
                )
            )
            {
                ZS.SetLevel(5);
                ZipEntry Path = new ZipEntry("UserSettings.json")
                {
                    IsUnicodeText = true,
                    DateTime = DateTime.Now,
                };
                ZS.PutNextEntry(Path);
                using (MemoryStream MS = new MemoryStream(Bytes))
                {
                    StreamUtils.Copy(MS, ZS, Buffer);
                }
                ZS.CloseEntry();
                ZS.IsStreamOwner = false;
                ZS.Finish();
                ZS.Close();
            }
        }
    }
}
