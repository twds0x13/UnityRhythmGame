using System;
using HoldNS;
using Game = GameManagerNS.GameManager;

namespace JudgeNS
{
    /// <summary>
    /// 长按音符判定时间阈值
    /// </summary>
    public class HoldJudgeTime
    {
        /// <summary> 精准完美 </summary>
        public const float CriticalPerfect = 0.024f;

        /// <summary> 完美 </summary>
        public const float Perfect = 0.048f;

        /// <summary> 优秀 </summary>
        public const float Great = 0.072f;

        /// <summary> 好 </summary>
        public const float Good = 0.096f;

        /// <summary> 错过 </summary>
        public const float Miss = 0.120f;
    }

    /// <summary>
    /// 长按音符判定得分系数
    /// </summary>
    public class HoldJudgeScore
    {
        /// <summary> 可得到的最高分数 </summary>
        public static float Max => CriticalPerfect;

        /// <summary> 精准完美 </summary>
        public const float CriticalPerfect = 1.1f;

        /// <summary> 完美 </summary>
        public const float Perfect = 1.0f;

        /// <summary> 优秀 </summary>
        public const float Great = 0.50f;

        /// <summary> 好 </summary>
        public const float Good = 0.20f;

        /// <summary> 错过 </summary>
        public const float Miss = 0.00f;
    }

    /// <summary>
    /// 长按音符判定逻辑
    /// </summary>
    public static class HoldJudge
    {
        /// <summary>
        /// 头部判定：根据时间差转换为判定等级
        /// </summary>
        /// <param name="Hold">长按音符对象</param>
        public static JudgeEnum GetHeadJudgeEnum(HoldBehaviour Hold) =>
            MathF.Abs(Hold.JudgeTime - Game.Inst.GetGameTime()) switch
            {
                < HoldJudgeTime.CriticalPerfect => JudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Perfect => JudgeEnum.Perfect,
                < HoldJudgeTime.Great => JudgeEnum.Great,
                < HoldJudgeTime.Good => JudgeEnum.Good,
                < HoldJudgeTime.Miss => JudgeEnum.Miss,
                > HoldJudgeTime.Miss => JudgeEnum.NotEntered,
                _ => throw new ArgumentOutOfRangeException(),
            };

        /// <summary>
        /// 尾部判定可调整是否宽松
        /// </summary>
        /// <param name="Hold">长按音符对象</param>
        /// <param name="tailJudge">严格判定 / 宽松判定 (在 Miss 区间以内统一 CriticalPerfect) </param>
        public static JudgeEnum GetTailJudgeEnum(HoldBehaviour Hold, bool tailJudge = false) =>
            MathF.Abs(Hold.JudgeTime + Hold.JudgeDuration - Game.Inst.GetGameTime()) switch
            {
                < HoldJudgeTime.CriticalPerfect => JudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Perfect => tailJudge
                    ? JudgeEnum.Perfect
                    : JudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Great => tailJudge ? JudgeEnum.Great : JudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Good => tailJudge ? JudgeEnum.Good : JudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Miss => JudgeEnum.Miss,
                > HoldJudgeTime.Miss => JudgeEnum.NotEntered,
                _ => throw new ArgumentOutOfRangeException(),
            };

        /// <summary>
        /// 判定等级转得分系数
        /// </summary>
        public static float GetJudgeScore(JudgeEnum Enum) =>
            Enum switch
            {
                JudgeEnum.CriticalPerfect => HoldJudgeScore.CriticalPerfect,
                JudgeEnum.Perfect => HoldJudgeScore.Perfect,
                JudgeEnum.Great => HoldJudgeScore.Great,
                JudgeEnum.Good => HoldJudgeScore.Good,
                JudgeEnum.Miss => HoldJudgeScore.Miss,
                JudgeEnum.NotEntered => HoldJudgeScore.Miss,
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}
