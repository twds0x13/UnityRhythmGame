using System;
using System.CodeDom;
using NoteNS;
using Game = GameManagerNS.GameManager;

namespace JudgeNS
{
    public class NoteJudgeTime
    {
        public const float CriticalPerfect = 0.016f;
        public const float Perfect = 0.032f;
        public const float Great = 0.064f;
        public const float Miss = 0.128f;
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

        public static NoteJudgeEnum GetJudgeEnum(NoteBehaviour Note) =>
            MathF.Abs(Note.JudgeTime - Game.Inst.GetGameTime()) switch
            {
                < NoteJudgeTime.CriticalPerfect => NoteJudgeEnum.CriticalPerfect,
                < NoteJudgeTime.Perfect => NoteJudgeEnum.Prefect,
                < NoteJudgeTime.Great => NoteJudgeEnum.Great,
                < NoteJudgeTime.Miss => NoteJudgeEnum.Miss,
                > NoteJudgeTime.Miss => NoteJudgeEnum.NotEntered,
                _ => throw new ArgumentOutOfRangeException(),
            };

        public static float GetJudgeScore(NoteJudgeEnum Enum) =>
            Enum switch
            {
                NoteJudgeEnum.CriticalPerfect => NoteJudgeScore.CriticalPerfect,
                NoteJudgeEnum.Prefect => NoteJudgeScore.Perfect,
                NoteJudgeEnum.Great => NoteJudgeScore.Great,
                NoteJudgeEnum.Miss => NoteJudgeScore.Miss,
                NoteJudgeEnum.NotEntered => throw new ArgumentNullException(),
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}
