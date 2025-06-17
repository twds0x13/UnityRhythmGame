using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using IUtils;
using Palmmedia.ReportGenerator.Core.Common;
using SFB;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Main = GameMain.GameMain;
using Rand = UnityEngine.Random;

namespace GameBehaviourManager
{
    /// <summary>
    /// 用来存储用户设置，默认参数是默认游戏设置
    /// </summary>
    public class GameSettings : IDev
    {
        internal GameSettings() { } // 两个数据类，默认只在GameMain里初始化

        public float JudgementTimeOffset = 0f;

        public float DisplayTimeOffset = 0f;

        public KeyCode KeyGamePause = KeyCode.W;

        public KeyCode KeyGameResume = KeyCode.W;

        public KeyCode KeyGameSave = KeyCode.Alpha1;

        public KeyCode KeyGameLoad = KeyCode.Alpha2;

        public KeyCode KeyGameTestNote = KeyCode.Alpha3;

        public KeyCode KeyGameTestTrack = KeyCode.Alpha4;

        public bool OneKey // 是不是用同一个按键来处理暂停和恢复
        {
            get { return KeyGamePause == KeyGameResume; }
        }

        public float TimeScaleSpeed
        {
            get { return GameTime.TimeScaleSpeed; }
            set { GameTime.TimeScaleSpeed = Mathf.Clamp(value, 0f, 100f); }
        }

        public void SetTimeScaleSpeed(float Speed)
        {
            TimeScaleSpeed = Speed;
        }

        public void DevLog()
        {
            Debug.LogFormat(
                "JTO:{0},DTO:{1},KGP:{2},KGR:{3},KGS:{4},KGL:{5},KGT:{6}",
                JudgementTimeOffset,
                DisplayTimeOffset,
                KeyGamePause.ToString(),
                KeyGameResume.ToString(),
                KeyGameSave
            );
        }
    }

    /// <summary>
    /// 用于处理 <see cref="Time.timeScale"/> 的全局更改
    /// </summary>
    internal class GameTime
    {
        public GameTime() { }

        public static Timer MainTimer;

        public static Timer RealTimer;

        public static bool TimePausing { get; set; }

        public static bool TimeResuming { get; set; }

        public static bool TimeStatFlip { get; set; } // 标记 暂停 / 恢复状态的翻转 [!] 不要尝试合并Pausing和Resuming这两个bool变量！情愿多加一个，能跑起来就行

        public static bool TimeChanging { get; set; }

        public static float TimeScaleStartingPoint { get; set; }

        public static float TimeScaleSpeed = 1f; // 暂停和继续游戏动画的速度倍数

        public static float TimeScale
        {
            get { return Time.timeScale; }
            private set { Time.timeScale = Mathf.Clamp(value, 0f, 100f); }
        }

        public static float TimeScaleCache = 0f;

        public static bool GamePaused { get; set; }

        /// <summary>
        /// <para>处理全局 <see cref="Time.timeScale"/> 变化（包括加减速，暂停等）相关的逻辑和按键检测。</para>
        /// 使用 <see cref="TimeScale"/> 作为索引器，保证不会越界修改 <see cref="Time.timeScale"/> 数值。
        /// </summary>
        public static void GlobalTimeUpdateHandler()
        {
            if (!Main.GameSettings.OneKey)
            {
                if (Input.GetKeyDown(Main.GameSettings.KeyGameResume))
                {
                    TimeChanging = true;
                    TimeResuming = true;
                    TimePausing = false;

                    TimeScaleCache = TimeScale;
                    TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
                }
                if (Input.GetKeyDown(Main.GameSettings.KeyGamePause))
                {
                    TimeChanging = true;
                    TimePausing = true;
                    TimeResuming = false;

                    TimeScaleCache = TimeScale;
                    TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
                }
            }
            else
            {
                if (Input.GetKeyDown(Main.GameSettings.KeyGamePause))
                {
                    if (TimeStatFlip)
                    {
                        TimeStatFlip = false; // 处于恢复中状态时为 false
                        TimeChanging = true;
                        TimePausing = false;
                        TimeResuming = true;
                    }
                    else
                    {
                        TimeStatFlip = true; // 处于暂停中状态为 true
                        TimeChanging = true;
                        TimePausing = true;
                        TimeResuming = false;
                    }

                    TimeScaleCache = TimeScale;
                    TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
                }
            }

            // START 玄学

            if (TimePausing)
            {
                TimeChanging = true;
                TimeScale =
                    TimeScaleSpeed
                    * (
                        TimeScaleStartingPoint
                        + (TimeScaleCache / TimeScaleSpeed)
                        - RealTimer.GetTimeElapsed()
                    );
                if (TimeScale == 0)
                {
                    GamePaused = true;
                    TimePausing = false;
                    TimeChanging = false;
                }
            }

            if (TimeResuming)
            {
                TimeChanging = true;
                GamePaused = false;
                TimeScale =
                    TimeScaleSpeed
                    * (RealTimer.GetTimeElapsed() + TimeScaleCache - TimeScaleStartingPoint);
                if (TimeScale >= 1)
                {
                    TimeScale = 1f;
                    TimeResuming = false;
                    TimeChanging = false;
                }
            }
            // END 玄学
        }

        public static void TimeUpdate()
        {
            GlobalTimeUpdateHandler();
        }
    }

    /// <summary>
    /// 用来管理游戏相关的行为，包括暂停，修改用户设置等。
    /// </summary>
    public class GameBehaviour : MonoBehaviour, IDev
    {
        private GameBehaviour() { } // 单例模式

        private static GameBehaviour GBinstance = null;

        public static GameBehaviour Inst
        {
            get { return GBinstance; }
        }

        private string UserSettingsZipPath = Application.streamingAssetsPath; // Zip 文件默认存放在 StreamingAssets 文件夹

        public void Awake()
        {
            InitInstance();

            InitGameTime();
        }

        private void InitInstance()
        {
            DontDestroyOnLoad(gameObject);

            if (GBinstance != null && GBinstance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                GBinstance = this;
            }
        }

        public void InitGameTime() // 最好把其他数据类的初始化扔到GameBehaviour里面，要不然容易出现初始化问题......
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
            GameTime.TimeUpdate();
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
            return GameTime.MainTimer.GetTimeElapsed() + Main.GameSettings.JudgementTimeOffset;
        }

        public float GetGameTimeMs()
        {
            return (GameTime.MainTimer.GetTimeElapsed() + Main.GameSettings.JudgementTimeOffset)
                * 1000;
        }

        public float GetAbsTimeMs()
        {
            return GameTime.RealTimer.GetTimeElapsed() * 1000;
        }

        public float GetAbsTime()
        {
            return GameTime.RealTimer.GetTimeElapsed();
        }

        public float GetTimeScale()
        {
            return GameTime.TimeScale;
        }

        public float GetTimeScaleCache()
        {
            return GameTime.TimeScaleCache;
        }

        public void SetTimeScaleSpeed(float Speed)
        {
            Main.GameSettings.SetTimeScaleSpeed(Speed);
        }

        public bool IsGamePaused()
        {
            return GameTime.GamePaused;
        }

        public void SaveGameSettings()
        {
            CompressToZipJson(Main.GameSettings, "UserSettings.zip");
        }

        public bool LoadGameSettings(ref GameSettings Object)
        {
            return LoadJsonFromZip<GameSettings>("Usersettings.zip", ref Object);
        }

        public bool CompressToZipJson<T>(T Object, string ZipFileName)
        {
            string Json = JsonUtility.ToJson(Object, true);
            byte[] Byte = Encoding.Unicode.GetBytes(Json);
            byte[] Buffer = new byte[2048];
            try
            {
                var FS = File.Create(Path.Join(UserSettingsZipPath, ZipFileName));
                using (ZipOutputStream ZS = new ZipOutputStream(FS))
                {
                    ZS.SetLevel(5);
                    ZipEntry Path = new ZipEntry("UserSettings.json")
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
            catch
            {
                Debug.Log("Failed to Save.");
                return false;
            }
        }

        public bool LoadJsonFromZip<T>(string ZipFileName, ref T RefObject)
        {
            try
            {
                using (
                    ZipInputStream ZS = new ZipInputStream(
                        File.OpenRead(Path.Join(UserSettingsZipPath, ZipFileName))
                    )
                )
                {
                    ZS.GetNextEntry();
                    byte[] ZipBuffer = new byte[ZS.Length];
                    ZS.Read(ZipBuffer, 0, ZipBuffer.Length);
                    string Json = Encoding.Unicode.GetString(ZipBuffer);
                    RefObject = JsonUtility.FromJson<T>(Json);
                    ZS.CloseEntry();
                    ZS.IsStreamOwner = false;
                    ZS.Close();
                    Debug.Log("Succesfully Loaded!");
                    return true;
                }
            }
            catch
            {
                Debug.Log("Failed to Load.");
                return false;
            }
        }
    }
}
