using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using IUtils;
using UnityEngine;

namespace GameBehaviourManager
{
    /// <summary>
    /// 用来存储用户设置，默认参数是默认游戏设置
    /// </summary>
    public class GameSettings
    {
        public float JudgementTimeOffset = 0f;

        public float DisplayTimeOffset = 0f;

        public KeyCode KeyGamePause = KeyCode.D;

        public KeyCode KeyGameResume = KeyCode.A;

        public float TimeScaleSpeed = 2f; // 暂停和继续游戏动画的速度倍数
    }

    /// <summary>
    /// 用来管理游戏相关的行为，包括暂停，修改用户设置等等
    /// </summary>
    public class GameBehaviour : MonoBehaviour, IDev, IGameBehaviour
    {
        private GameBehaviour() { }

        private static GameBehaviour GBinstance = null;

        public static GameBehaviour Inst
        {
            get { return GBinstance; }
        } // 用了单例模式

        private static GameSettings GameSettings = new GameSettings();
        public static float TimeScaleStartingPoint { get; private set; }

        public static float TimeScale
        {
            get { return Time.timeScale; }
            private set { Time.timeScale = Mathf.Clamp(value, 0f, 100f); }
        }

        public static bool TimePausing { get; private set; }

        public static bool TimeResuming { get; private set; }

        private string UserSettingsZipPath = Application.streamingAssetsPath; // Zip 文件默认存放在 StreamingAssets 文件夹内

        private static Timer MainTimer;

        private static Timer RealTimer;

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

            RealTimer = Timer.Register(
                duration: 1145141919810f,
                onComplete: null,
                useRealTime: true
            );
        }

        public void Update()
        {
            //GamePauseHandler();
            //GameResumeHandler();
            CoolerGamePauseHandler();
            CoolerGameResumeHandler();
        }

        public void DevLog()
        {
            Debug.LogFormat(
                "Mono Behaviour Load Time Usage: {0} ms",
                (RealTimer.GetTimeElapsed() - MainTimer.GetTimeElapsed()) * 1000
            );
        }

        public float GameTime()
        {
            return MainTimer.GetTimeElapsed() + GameSettings.JudgementTimeOffset;
        }

        public float GameTimeMs()
        {
            return (MainTimer.GetTimeElapsed() + GameSettings.JudgementTimeOffset) * 1000;
        }

        public float AbsTimeMs()
        {
            return RealTimer.GetTimeElapsed() * 1000; // 输出毫秒
        }

        public float AbsTime()
        {
            return RealTimer.GetTimeElapsed();
        }

        public void CoolerGameResumeHandler()
        {
            if (
                Input.GetKeyDown(GameSettings.KeyGameResume)
                && GamePaused
                && !TimeResuming
                && !TimePausing
            )
            {
                GamePaused = false;
                TimeResuming = true;
                TimeScaleStartingPoint = AbsTime();
            }

            if (TimeResuming)
            {
                TimeScale = GameSettings.TimeScaleSpeed * (AbsTime() - TimeScaleStartingPoint);
                if (TimeScale >= 1)
                {
                    TimeScale = 1f;
                    TimeResuming = false;
                }
            }
        }

        public void CoolerGamePauseHandler()
        {
            if (
                Input.GetKeyDown(GameSettings.KeyGamePause)
                && !GamePaused
                && !TimePausing
                && !TimeResuming
            )
            {
                GamePaused = true;
                TimePausing = true;
                TimeScaleStartingPoint = AbsTime();
            }

            if (TimePausing)
            {
                TimeScale =
                    GameSettings.TimeScaleSpeed
                    * (TimeScaleStartingPoint + (1f / GameSettings.TimeScaleSpeed) - AbsTime());
                if (TimeScale == 0)
                {
                    TimePausing = false;
                }
            }
        }

        public void Pause()
        {
            GamePaused = true;
            Time.timeScale = 1f;
            MainTimer.Pause();
        }

        public void Resume()
        {
            GamePaused = false;
            Time.timeScale = 1f;
            MainTimer.Resume();
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
