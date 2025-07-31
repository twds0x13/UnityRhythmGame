using System;
using NoteNamespace;
using Game = GameManager.GameManager;

namespace NoteJudge
{
    public class NoteJudgeTime
    {
        public const float Perfect = 0.040f;
        public const float Great = 0.080f;
        public const float Good = 0.120f;
        public const float Miss = 0.160f;
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
    }
}
