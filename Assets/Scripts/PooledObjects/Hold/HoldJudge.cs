using System;
using HoldNS;
using Game = GameManagerNS.GameManager;

namespace HoldJudgeNS
{
    /// <summary>
    /// 长按音符判定时间阈值
    /// </summary>
    public class HoldJudgeTime
    {
        /// <summary> 精准完美 </summary>
        public const float CriticalPerfect = 0.032f;

        /// <summary> 完美 </summary>
        public const float Perfect = 0.064f;

        /// <summary> 优秀 </summary>
        public const float Great = 0.096f;

        /// <summary> 错过 </summary>
        public const float Miss = 0.128f;
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

        /// <summary> 错过 </summary>
        public const float Miss = 0.00f;
    }

    /// <summary>
    /// 长按音符判定逻辑
    /// </summary>
    public static class HoldJudge
    {
        /// <summary>
        /// 判定等级枚举
        /// </summary>
        public enum HoldJudgeEnum
        {
            CriticalPerfect, // 精准完美
            Perfect, // 完美
            Great, // 优秀
            Miss, // 错过
            NotEntered, // 未进入判定区间
        }

        /// <summary>
        /// 头部判定：根据时间差转换为判定等级
        /// </summary>
        /// <param name="Hold">长按音符对象</param>
        public static HoldJudgeEnum GetHeadJudgeEnum(HoldBehaviour Hold) =>
            MathF.Abs(Hold.JudgeTime - Game.Inst.GetGameTime()) switch
            {
                < HoldJudgeTime.CriticalPerfect => HoldJudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Perfect => HoldJudgeEnum.Perfect,
                < HoldJudgeTime.Great => HoldJudgeEnum.Great,
                < HoldJudgeTime.Miss => HoldJudgeEnum.Miss,
                > HoldJudgeTime.Miss => HoldJudgeEnum.NotEntered,
                _ => throw new ArgumentOutOfRangeException(),
            };

        /// <summary>
        /// 尾部判定可调整是否宽松
        /// </summary>
        /// <param name="Hold">长按音符对象</param>
        /// <param name="tailJudge">严格判定 / 宽松判定 (在 Miss 区间以内统一 CriticalPerfect) </param>
        public static HoldJudgeEnum GetTailJudgeEnum(HoldBehaviour Hold, bool tailJudge = false) =>
            MathF.Abs((Hold.JudgeTime + Hold.JudgeDuration) - Game.Inst.GetGameTime()) switch
            {
                < HoldJudgeTime.CriticalPerfect => HoldJudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Perfect => tailJudge
                    ? HoldJudgeEnum.Perfect
                    : HoldJudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Great => tailJudge
                    ? HoldJudgeEnum.Great
                    : HoldJudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Miss => tailJudge
                    ? HoldJudgeEnum.Miss
                    : HoldJudgeEnum.CriticalPerfect,
                > HoldJudgeTime.Miss => HoldJudgeEnum.NotEntered,
                _ => throw new ArgumentOutOfRangeException(),
            };

        /// <summary>
        /// 判定等级转得分系数
        /// </summary>
        public static float GetJudgeScore(HoldJudgeEnum Enum) =>
            Enum switch
            {
                HoldJudgeEnum.CriticalPerfect => HoldJudgeScore.CriticalPerfect,
                HoldJudgeEnum.Perfect => HoldJudgeScore.Perfect,
                HoldJudgeEnum.Great => HoldJudgeScore.Great,
                HoldJudgeEnum.Miss => HoldJudgeScore.Miss,
                HoldJudgeEnum.NotEntered => throw new ArgumentNullException(),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}
