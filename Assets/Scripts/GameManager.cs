using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Singleton;
using UnityEngine;

namespace GameManager
{
    /// <summary>
    /// 用来存储用户设置，默认参数是默认游戏设置
    /// </summary>
    #region GameSettings
    public class GameSettings
    {
        internal GameSettings() { } // 两个数据类，默认只在GameMain里初始化

        public float JudgementTimeOffset = 0f;

        public float DisplayTimeOffset = 0f;

        public KeyCode KeyGamePause = KeyCode.Escape;

        public KeyCode KeyGameResume = KeyCode.Escape;

        public KeyCode KeyGameSave = KeyCode.Alpha1;

        public KeyCode KeyGameLoad = KeyCode.Alpha2;

        public KeyCode KeyGameTestNote = KeyCode.Alpha3;

        public KeyCode KeyGameTestTrack = KeyCode.Alpha4;

        public KeyCode[] KeyGameplay =
        {
            KeyCode.Q,
            KeyCode.W,
            KeyCode.LeftBracket,
            KeyCode.RightBracket,
        };

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

        public void SetTimeScale(float Speed)
        {
            GameTime.TimeScale = Speed;
        }
    }

    #endregion

    /// <summary>
    /// 内嵌类，用于处理 <see cref="Time.timeScale"/> 的全局更改
    /// </summary>
    #region GameTime
    internal class GameTime
    {
        public GameTime() { }

        public static Timer MainTimer;

        public static Timer RealTimer;

        public static bool IgnorePause;

        public static bool TimePausing { get; set; }

        public static bool TimeResuming { get; set; }

        public static bool TimeStatFlip { get; set; } // 标记 暂停 / 恢复状态的翻转  [!] 不要尝试合并Pausing和Resuming这两个bool变量！情愿多加一个，能跑起来就行

        public static bool TimeChanging { get; set; }

        public static float TimeScaleStartingPoint { get; set; }

        public static float TimeScaleSpeed = 1f; // 暂停和继续游戏动画的速度倍数

        public static float TimeScale
        {
            get { return Time.timeScale; }
            internal set { Time.timeScale = Mathf.Clamp(value, 0f, 100f); }
        }

        public static float TimeScaleCache = 1f;

        public static bool GamePaused { get; set; }

        /// <summary>
        /// <para>处理全局 <see cref="Time.timeScale"/> 变化（包括加减速，暂停等）相关的逻辑和按键检测</para>
        /// 使用 <see cref="TimeScale"/> 作为索引器，保证不会越界修改 <see cref="Time.timeScale"/> 数值
        /// </summary>
        public static void OnTimeUpdate()
        {
            if (IgnorePause)
            {
                return;
            }

            if (!GameManager.Inst.Settings.OneKey)
            {
                if (Input.GetKeyDown(GameManager.Inst.Settings.KeyGameResume))
                {
                    TimeChanging = true;
                    TimeResuming = true;
                    TimePausing = false;

                    TimeScaleCache = TimeScale;
                    TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
                }
                if (Input.GetKeyDown(GameManager.Inst.Settings.KeyGamePause))
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
                if (Input.GetKeyDown(GameManager.Inst.Settings.KeyGamePause))
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

            if (TimePausing)
            {
                TimeChanging = true;
                TimeScale =
                    (TimeScaleStartingPoint + TimeScaleCache - RealTimer.GetTimeElapsed())
                    / TimeScaleSpeed;
                TimeScale = Mathf.Clamp01(TimeScale);

                /* 时间流速解析算法：
                 *
                 * 在上面记录一次当前时间点 存到 TimeScaleStartingPoint
                 *
                 * 先把 TimeScaleSpeed 记为 1f
                 *
                 * 我们得到了这个公式 : TimeScale = 起点时间 + 上一次状态翻转的时间流速 - 现在真实时间
                 *
                 * 第一次按下暂停时记录上一次状态翻转的时间流速为1f
                 *
                 * 继续简化公式 : TimeScale = 起点时间 + 1f - 现在真实时间
                 *
                 * 所以TimeScale 随现在时间增大而降低，在1秒后到达0。
                 *
                 * 如果 TimeScaleSpeed = 2f (更快)，那么 TimeScale 将以两倍的速度衰减到 0，所以持续时间是 TimeScaleCache / 2。
                 *
                 * 如果 TimeScaleSpeed = 0.5f (更慢)，那么 TimeScale 将以一半的速度衰减到 0，所以持续时间是 TimeScaleCache / 0.5 (即 TimeScaleCache * 2)。
                 *
                 * TimeScaleSpeed 越大，衰减过程越快，持续时间越短；TimeScaleSpeed 越小，衰减过程越慢，持续时间越长。
                 */

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
                    (RealTimer.GetTimeElapsed() + TimeScaleCache - TimeScaleStartingPoint)
                    / TimeScaleSpeed;
                TimeScale = Mathf.Clamp01(TimeScale);
                if (TimeScale >= 1)
                {
                    TimeScale = 1f;
                    TimeResuming = false;
                    TimeChanging = false;
                }
            }
        }

        public static void TimeUpdate()
        {
            OnTimeUpdate();
        }
    }

    #endregion

    /// <summary>
    /// 用来管理游戏相关的行为，包括暂停，修改用户设置等。
    /// 继承自全局单例基类 <see cref="Singleton{T}"/>
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public GameSettings Settings = new(); // TODO : 改成 protected 或 private

        private string UserSettingsZipPath;

        protected override void SingletonAwake()
        {
            InitGameTime();

            InitPath();
        }

        private void InitPath()
        {
            UserSettingsZipPath = Application.persistentDataPath;
        }

        public void InitGameTime() // 记得把其他数据类的初始化扔到GameBehaviour里面
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

        public float GetGameTime() =>
            GameTime.MainTimer.GetTimeElapsed() + Inst.Settings.JudgementTimeOffset;

        public float GetAbsTime() => GameTime.RealTimer.GetTimeElapsed();

        public float GetTimeScale() => GameTime.TimeScale;

        public float GetTimeScaleCache() => GameTime.TimeScaleCache;

        public void LockTimeScale(float Speed) // 时间流速被强制设置的时候不让暂停（诡谲）
        {
            GameTime.IgnorePause = true;

            Inst.Settings.SetTimeScale(Speed);
        }

        public void UnlockTimeScale() => GameTime.IgnorePause = false; // 解锁强制时间流速设置，恢复到正常状态

        public bool IsGamePaused() => GameTime.GamePaused;

        public void SaveGameSettings() => CompressToZipJson(Inst.Settings, "UserSettings.zip");

        public bool LoadGameSettings(ref GameSettings Object) =>
            LoadJsonFromZip("Usersettings.zip", ref Object);

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
