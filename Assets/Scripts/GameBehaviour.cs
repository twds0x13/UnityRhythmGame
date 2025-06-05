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
    internal class GameSettings
    {
        private GameSettings() { } // 两个数据类

        private static GameSettings GBinstance = null;

        public static float JudgementTimeOffset = 0f;

        public static float DisplayTimeOffset = 0f;

        public static KeyCode KeyGamePause = KeyCode.D;

        public static KeyCode KeyGameResume = KeyCode.A;

        public static float TimeScaleSpeed
        {
            get { return GameTime.TimeScaleSpeed; }
            set { GameTime.TimeScaleSpeed = Mathf.Clamp(value, 0f, 100f); }
        }

        public static void SetTimeScaleSpeed(float Speed)
        {
            TimeScaleSpeed = Speed;
        }

        public static GameSettings Inst()
        {
            return GBinstance;
        }
    }

    /// <summary>
    /// 用于处理游戏时间流速的全局更改
    /// </summary>
    internal class GameTime
    {
        private GameTime() { }

        public static Timer MainTimer;

        public static Timer RealTimer;

        public static bool TimePausing { get; set; }

        public static bool TimeResuming { get; set; }

        public static bool TimeChanging { get; set; }

        public static float TimeScaleStartingPoint { get; set; }

        public static float TimeScaleSpeed = 1f; // 暂停和继续游戏动画的速度倍数

        public static float TimeScale
        {
            get { return Time.timeScale; }
            set { Time.timeScale = Mathf.Clamp(value, 0f, 100f); }
        }

        public static float TimeScaleCache = 0f;

        public static bool GamePaused { get; set; }

        public static void TimeUpdateManager()
        {
            if (TimePausing)
            {
                TimeScale =
                    TimeScaleSpeed
                    * (
                        TimeScaleStartingPoint
                        + (TimeScaleCache / TimeScaleSpeed)
                        - RealTimer.GetTimeElapsed()
                    );
                if (TimeScale == 0)
                {
                    TimePausing = false;
                }
            }

            if (TimeResuming)
            {
                TimeScale =
                    TimeScaleSpeed
                    * (RealTimer.GetTimeElapsed() + TimeScaleCache - TimeScaleStartingPoint);
                if (TimeScale >= 1)
                {
                    TimeScale = 1f;
                    TimeResuming = false;
                }
            }
        }

        public static void CoolerGameResumeHandler()
        {
            if (Input.GetKeyDown(GameSettings.KeyGameResume))
            {
                TimeChanging = true;
                TimeResuming = true;
                TimePausing = false;
                TimeScaleCache = TimeScale;
                TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
            }
        }

        public static void CoolerGamePauseHandler()
        {
            if (Input.GetKeyDown(GameSettings.KeyGamePause))
            {
                TimeChanging = true;
                TimePausing = true;
                TimeResuming = false;
                TimeScaleCache = TimeScale;
                TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
            }
        }
    }

    /// <summary>
    /// 用来管理游戏相关的行为，包括暂停，修改用户设置等等
    /// </summary>
    public class GameBehaviour : MonoBehaviour, IDev, IGameBehaviour
    {
        private GameBehaviour() { } // 单例模式

        private static GameBehaviour GBinstance = null;

        public static GameBehaviour Inst
        {
            get { return GBinstance; }
        }

        private string UserSettingsZipPath = Application.streamingAssetsPath; // Zip 文件默认存放在 StreamingAssets 文件夹内

        public void Awake()
        {
            InitInstance();

            InitGameTime();
        }

        private void InitInstance()
        {
            DontDestroyOnLoad(this.gameObject);

            if (GBinstance != null && GBinstance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                GBinstance = this;
            }
        }

        private void InitGameTime()
        {
            GameTime.MainTimer = Timer.Register(
                duration: 1145141919810f,
                onComplete: null,
                useRealTime: false
            );

            GameTime.RealTimer = Timer.Register(
                duration: 1145141919810f,
                onComplete: null,
                useRealTime: true
            );
        }

        public void Update()
        {
            //GamePauseHandler();
            //GameResumeHandler();
            GameTime.CoolerGamePauseHandler();
            GameTime.CoolerGameResumeHandler();
            GameTime.TimeUpdateManager();
        }

        public void DevLog()
        {
            Debug.LogFormat(
                "Mono Behaviour Load Time Usage: {0} ms",
                (GameTime.RealTimer.GetTimeElapsed() - GameTime.MainTimer.GetTimeElapsed()) * 1000
            );
        }

        public float GetGameTime()
        {
            return GameTime.MainTimer.GetTimeElapsed() + GameSettings.JudgementTimeOffset;
        }

        public float GetGameTimeMs()
        {
            return (GameTime.MainTimer.GetTimeElapsed() + GameSettings.JudgementTimeOffset) * 1000;
        }

        public float GetAbsTimeMs()
        {
            return GameTime.RealTimer.GetTimeElapsed() * 1000; // 输出毫秒
        }

        public float GetAbsTime()
        {
            return GameTime.RealTimer.GetTimeElapsed();
        }

        public float GetTimeScale()
        {
            return GameTime.TimeScale;
        }

        public float GetTimeScaleCache() // 只供测试使用
        {
            return GameTime.TimeScaleCache;
        }

        public void SetTimeScaleSpeed(float Speed)
        {
            GameSettings.SetTimeScaleSpeed(Speed);
        }

        public bool IsGamePaused()
        {
            return GameTime.GamePaused;
        }

        public void Pause()
        {
            GameTime.GamePaused = true;
            Time.timeScale = 1f;
            GameTime.MainTimer.Pause();
        }

        public void Resume()
        {
            GameTime.GamePaused = false;
            Time.timeScale = 1f;
            GameTime.MainTimer.Resume();
        }

        public void SaveGameSettings()
        {
            string UserSettingsJson = JsonUtility.ToJson(GameSettings.Inst());
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
