using System;
using JudgeNS;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using Singleton;
using UnityEngine;
using Pool = PooledObjectNS.PooledObjectManager;

namespace GameManagerNS
{
    /// <summary>
    /// 静态设置类
    /// </summary>
    #region GameSettings
    public static class GameSettings
    {
        /// <summary>
        /// 谱面显示的垂直缩放。缩放的坐标原点为 Track 的正中央。
        /// 这个值增大时 Note 会从屏幕外更高的位置开始下落、更快速的落到 Track 上、并且停留在屏幕内的时间更短
        /// </summary>
        public static float ChartVerticalScale { get; private set; } = 1f;

        /// <summary>
        /// Note 从屏幕外一点开始运动到 Track 正中央所花费的预定时间
        /// </summary>
        public static float NoteFallDuration { get; internal set; } = 0.60f;

        public static float JudgeDisplayerHeight { get; internal set; } = 0.03f;
    }

    #endregion

    /// <summary>
    /// 游戏分数
    /// </summary>
    #region GameScore
    public class GameScore
    {
        internal GameScore() { }

        float _score; // 内部当前分数 （ 按照得分倍率计算 ）

        float _maxScore; // 内部最大分数（ 全大 Perfect ）

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

        public float MaxCombo { get; set; }

        public float CurrentCombo { get; set; }
    }
    #endregion

    /// <summary>
    /// 内嵌类，用于处理 <see cref="Time.timeScale"/> 的全局更改
    /// </summary>
    #region GameTime
    public class GameTime
    {
        internal GameTime() { }

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

        public static bool IsGamePaused { get; private set; }

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
                    IsGamePaused = true;
                    TimePausing = false;
                    TimeChanging = false;
                }
            }

            if (TimeResuming)
            {
                TimeChanging = true;
                IsGamePaused = false;
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
        public readonly GameScore Score = new();

        protected override void SingletonAwake()
        {
            QualitySettings.vSyncCount = 1;

            QualitySettings.antiAliasing = 4;

            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

            QualitySettings.globalTextureMipmapLimit = 0;

            DetectMaxFrameRate();

            InitGameTime();
        }

        public void DetectMaxFrameRate()
        {
            double currentRefreshRate = Screen.currentResolution.refreshRateRatio.value;
            Debug.Log($"屏幕刷新率: {currentRefreshRate} Hz");

            double maxRefreshRate = GetMaxSupportedRefreshRate();
            Debug.Log($"设备支持的最大刷新率: {maxRefreshRate} Hz");

            Application.targetFrameRate = (int)Math.Floor(currentRefreshRate);
        }

        private double GetMaxSupportedRefreshRate()
        {
            double maxRefreshRate = 0f;
            Resolution[] resolutions = Screen.resolutions;

            foreach (Resolution res in resolutions)
            {
                double refreshRate = res.refreshRateRatio.value;
                if (refreshRate > maxRefreshRate)
                {
                    maxRefreshRate = refreshRate;
                }
            }

            // 如果获取失败，使用当前刷新率
            return maxRefreshRate > 0
                ? maxRefreshRate
                : Screen.currentResolution.refreshRateRatio.value;
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
        public float GetGameTime() => GameTime.MainTimer.GetTimeElapsed();

        public float GetAbsTime() => GameTime.RealTimer.GetTimeElapsed();

        public float GetTimeScale() => GameTime.TimeScale;

        public float GetTimeScaleCache() => GameTime.TimeScaleCache;

        public bool IsGamePaused() => GameTime.IsGamePaused;

        // public void PauseGame() => GameTime.SwitchToPause();

        // public void ResumeGame() => GameTime.SwitchToResume();

        public void PauseResumeGame() => GameTime.OnPauseResume();

        /// <summary>
        /// 简易的包装方法，根据传入的判定枚举和对象类型，增加对应的分数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="judgeEnum"></param>
        public void AddScore<T>(JudgeEnum judgeEnum)
            where T : class
        {
            if (judgeEnum != JudgeEnum.Miss || judgeEnum != JudgeEnum.NotEntered)
            {
                Score.CurrentCombo += 1;
                Score.MaxCombo = Mathf.Max(Score.MaxCombo, Score.CurrentCombo);
            }
            else
            {
                Score.CurrentCombo = 0;
            }

            if (typeof(T) == typeof(NoteNS.NoteBehaviour))
            {
                Score.Score += NoteJudge.GetJudgeScore(judgeEnum);
                Score.MaxScore += NoteJudgeScore.Max;
            }
            else if (typeof(T) == typeof(HoldNS.HoldBehaviour))
            {
                Score.Score += HoldJudge.GetJudgeScore(judgeEnum);
                Score.MaxScore += HoldJudgeScore.Max;
            }
        }

        public void StartGame()
        {
            Pool.Inst.StartGame();
        }

        public void ExitGame()
        {
            Pool.Inst.ExitGame();

            Score.MaxScore = 0f;
            Score.Score = 0f;
            Score.MaxCombo = 0f;
            Score.CurrentCombo = 0f;
        }
    }
}
