using JsonLoader;
using Singleton;
using UnityEngine;
using Json = JsonLoader.BaseJsonLoader;
using Pool = PooledObjectNS.PooledObjectManager;

namespace GameManagerNS
{
    /// <summary>
    /// 用来存一些新版输入和其他不方便存的东西
    /// </summary>
    #region GameSettings
    public class GameSettings
    {
        internal GameSettings() { } // 两个数据类，默认只在GameMain里初始化

        public float JudgementTimeOffset = 0f;

        public float DisplayTimeOffset = 0f;

        public float MusicTimeOffset = 0f;
    }

    #endregion

    /// <summary>
    /// 游戏分数
    /// </summary>
    #region GameScore
    public class GameScore
    {
        internal GameScore() { }

        float _score; // 当前分数

        float _maxScore; // 理论最大分数（全 Perfect）

        public float Accuracy // 完成率
        {
            get
            {
                if (_maxScore != 0f)
                {
                    return _score / _maxScore;
                }
                else
                {
                    return 1f;
                }
            }
        }
        public float Score
        {
            get { return _score; }
            set { _score = value; }
        }

        public float MaxScore
        {
            get { return _maxScore; }
            set { _maxScore = value; }
        }
    }
    #endregion

    /// <summary>
    /// 内嵌类，用于处理 <see cref="Time.timeScale"/> 的全局更改
    /// </summary>
    #region GameTime
    internal class GameTime
    {
        private GameTime() { }

        public static Timer MainTimer;

        public static Timer RealTimer;

        public static bool IgnorePause;

        public static bool TimePausing { get; private set; }

        public static bool TimeResuming { get; private set; }

        public static bool TimeStatFlip { get; private set; } // 标记 暂停 / 恢复状态的翻转  [!] 不要尝试合并 Pausing 和 Resuming 这两个 bool 变量！能跑起来就行

        public static bool TimeChanging { get; private set; }

        public static float TimeScaleStartingPoint { get; private set; }

        public static float TimeScaleSpeed = 2.25f; // 暂停和继续游戏动画的速度倍数

        public static float TimeScale
        {
            get { return Time.timeScale; }
            internal set { Time.timeScale = Mathf.Clamp(value, 0f, 100f); }
        }

        public static float TimeScaleCache = 1f;

        public static bool GamePaused { get; private set; }

        /// <summary>
        /// 处理全局 <see cref="Time.timeScale"/> 变化（包括加减速，暂停等）相关的逻辑和按键检测。
        /// 使用 <see cref="TimeScale"/> 作为索引器，保证不会越界修改 <see cref="Time.timeScale"/> 数值
        /// </summary>
        private static void OnTimeUpdate()
        {
            if (IgnorePause)
            {
                return;
            }

            if (TimePausing)
            {
                TimeChanging = true;
                TimeScale =
                    (TimeScaleStartingPoint - RealTimer.GetTimeElapsed()) * TimeScaleSpeed
                    + TimeScaleCache;

                if (TimeScale <= 0)
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
                    (RealTimer.GetTimeElapsed() - TimeScaleStartingPoint) * TimeScaleSpeed
                    + TimeScaleCache;

                if (TimeScale >= 1)
                {
                    TimeScale = 1f;
                    TimeResuming = false;
                    TimeChanging = false;
                }
            }
        }

        public static void SwitchToResume()
        {
            TimeChanging = true;
            TimePausing = false;
            TimeResuming = true;
            TimeScaleCache = TimeScale;
            TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
        }

        public static void SwitchToPause()
        {
            TimeChanging = true;
            TimePausing = true;
            TimeResuming = false;
            TimeScaleCache = TimeScale;
            TimeScaleStartingPoint = RealTimer.GetTimeElapsed();
        }

        private static void OnOneKeyPause()
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

        // 随时随地能按暂停对游戏外没用，游戏内使用 HoverPage (弹窗) 调用 SwitchToResume() 代替

        public static void OnPauseResume()
        {
            OnOneKeyPause();
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
        public GameScore Score = new();

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

        private void Update()
        {
            GameTime.TimeUpdate();
        }

        /// <summary>
        /// 返回游戏时间（秒）
        /// </summary>
        /// <returns></returns>
        public float GetGameTime() =>
            GameTime.MainTimer.GetTimeElapsed() + Inst.Settings.JudgementTimeOffset;

        public float GetAbsTime() => GameTime.RealTimer.GetTimeElapsed();

        public float GetTimeScale() => GameTime.TimeScale;

        public float GetTimeScaleCache() => GameTime.TimeScaleCache;

        public void LockTimeScale() // 时间流速被强制设置的时候不让暂停（诡谲）
        {
            GameTime.IgnorePause = true;

            // Inst.Settings.SetTimeScale(Speed);
        }

        public void UnlockTimeScale() => GameTime.IgnorePause = false; // 解锁强制时间流速设置，恢复到正常状态

        public bool IsGamePaused() => GameTime.GamePaused;

        public void PauseGame() => GameTime.SwitchToPause();

        public void ResumeGame() => GameTime.SwitchToResume();

        public void PauseResumeGame() => GameTime.OnPauseResume();

        public void SaveGameSettings() => Json.SaveObject("UserSettings.zip", Inst.Settings);

        public bool LoadGameSettings(ref GameSettings Object)
        {
            var (success, result) = Json.LoadObject<GameSettings>("UserSettings.zip");

            if (success)
            {
                Object = result;
            }

            return success;
        }

        public void StartGame()
        {
            Pool.Inst.GetTracksDynamic();
        }

        public void ExitGame()
        {
            Score.MaxScore = 0f;
            Score.Score = 0f;
        }
    }
}
