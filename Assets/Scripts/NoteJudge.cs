using System;
using NoteNS;
using Game = GameManagerNS.GameManager;

namespace NoteJudgeNS
{
    public class NoteJudgeTime
    {
        public const float Perfect = 0.040f;
        public const float Great = 0.080f;
        public const float Good = 0.120f;
        public const float Miss = 0.160f;
    }

    public class NoteJudgeScore
    {
        public static float Max
        {
            get { return Perfect; }
        }
        public const float Perfect = 1.0f;
        public const float Great = 0.66f;
        public const float Good = 0.30f;
        public const float Miss = 0.00f;
    }

    public class NoteJudge
    {
        public enum NoteJudgeEnum
        {
            Perfect,
            Great,
            Good,
            Miss,
            NotEntered, // 代表尚未进入判定区间
        }

        public static NoteJudgeEnum GetJudgeEnum(NoteBehaviour Note) // 非 Miss
        {
            var Tmp = Math.Abs(Game.Inst.GetGameTime() - Note.JudgeTime);

            if (Tmp < NoteJudgeTime.Perfect)
            {
                return NoteJudgeEnum.Perfect;
            }
            if (Tmp < NoteJudgeTime.Great)
            {
                return NoteJudgeEnum.Great;
            }
            if (Tmp < NoteJudgeTime.Good)
            {
                return NoteJudgeEnum.Good;
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
                case NoteJudgeEnum.Perfect:
                    return NoteJudgeScore.Perfect;
                case NoteJudgeEnum.Great:
                    return NoteJudgeScore.Great;
                case NoteJudgeEnum.Good:
                    return NoteJudgeScore.Good;
                case NoteJudgeEnum.Miss:
                    return NoteJudgeScore.Miss;
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
