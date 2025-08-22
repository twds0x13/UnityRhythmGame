using System;
using NoteNS;
using Game = GameManagerNS.GameManager;

namespace NoteJudgeNS
{
    public class NoteJudgeTime
    {
        public const float CriticalPerfect = 0.016f;
        public const float Perfect = 0.032f;
        public const float Great = 0.064f;
        public const float Miss = 0.100f;
    }

    public class NoteJudgeScore
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

    public class NoteJudge
    {
        public enum NoteJudgeEnum
        {
            CriticalPerfect,
            Prefect,
            Great,
            Miss,
            NotEntered, // 代表尚未进入判定区间
        }

        public static NoteJudgeEnum GetJudgeEnum(NoteBehaviour Note) // 非 Miss
        {
            var Tmp = Math.Abs(Game.Inst.GetGameTime() - Note.JudgeTime);

            if (Tmp < NoteJudgeTime.CriticalPerfect)
            {
                return NoteJudgeEnum.CriticalPerfect;
            }
            if (Tmp < NoteJudgeTime.Perfect)
            {
                return NoteJudgeEnum.Prefect;
            }
            if (Tmp < NoteJudgeTime.Great)
            {
                return NoteJudgeEnum.Great;
            }
            if (Tmp < NoteJudgeTime.Miss)
            {
                return NoteJudgeEnum.Miss;
            }

            return NoteJudgeEnum.NotEntered;
        }

        public static float GetJudgeScore(NoteJudgeEnum Enum)
        {
            switch (Enum)
            {
                case NoteJudgeEnum.CriticalPerfect:
                    return NoteJudgeScore.CriticalPerfect;
                case NoteJudgeEnum.Prefect:
                    return NoteJudgeScore.Perfect;
                case NoteJudgeEnum.Great:
                    return NoteJudgeScore.Great;
                case NoteJudgeEnum.Miss:
                    return NoteJudgeScore.Miss;
                case NoteJudgeEnum.NotEntered: // This should not happen.
                    return NoteJudgeScore.Miss;
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
