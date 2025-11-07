using System;
using System.CodeDom;
using HoldNS;
using Game = GameManagerNS.GameManager;

namespace HoldJudgeNS
{
    public class HoldJudgeTime
    {
        public const float CriticalPerfect = 0.016f;
        public const float Perfect = 0.032f;
        public const float Great = 0.064f;
        public const float Miss = 0.100f;
    }

    public class HoldJudgeScore
    {
        public static float Max
        {
            get { return CriticalPerfect; }
        }
        public const float CriticalPerfect = 1.1f;
        public const float Perfect = 1.0f;
        public const float Great = 0.50f;
        public const float Miss = 0.00f;
    }

    public class HoldJudge
    {
        public enum HoldJudgeEnum
        {
            CriticalPerfect,
            Prefect,
            Great,
            Miss,
            NotEntered, // 代表尚未进入判定区间
        }

        public static HoldJudgeEnum GetJudgeEnum(HoldBehaviour Hold) =>
            MathF.Abs(Hold.JudgeTime - Game.Inst.GetGameTime()) switch
            {
                < HoldJudgeTime.CriticalPerfect => HoldJudgeEnum.CriticalPerfect,
                < HoldJudgeTime.Perfect => HoldJudgeEnum.Prefect,
                < HoldJudgeTime.Great => HoldJudgeEnum.Great,
                < HoldJudgeTime.Miss => HoldJudgeEnum.Miss,
                > HoldJudgeTime.Miss => HoldJudgeEnum.NotEntered,
                _ => throw new ArgumentOutOfRangeException(),
            };

        public static float GetJudgeScore(HoldJudgeEnum Enum) =>
            Enum switch
            {
                HoldJudgeEnum.CriticalPerfect => HoldJudgeScore.CriticalPerfect,
                HoldJudgeEnum.Prefect => HoldJudgeScore.Perfect,
                HoldJudgeEnum.Great => HoldJudgeScore.Great,
                HoldJudgeEnum.Miss => HoldJudgeScore.Miss,
                HoldJudgeEnum.NotEntered => throw new ArgumentNullException(),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}
