using System;
using NoteNS;
using Game = GameManagerNS.GameManager;

namespace JudgeNS
{
    public enum JudgeEnum
    {
        CriticalPerfect,
        Perfect,
        Great,
        Good,
        Miss,
        NotEntered, // 代表尚未进入判定区间
    }

    public class NoteJudgeTime
    {
        public const float CriticalPerfect = 0.024f;
        public const float Perfect = 0.048f;
        public const float Great = 0.072f;
        public const float Good = 0.096f;
        public const float Miss = 0.120f;
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
        public const float Good = 0.20f;
        public const float Miss = 0.00f;
    }

    public class NoteJudge
    {
        public static JudgeEnum GetJudgeEnum(NoteBehaviour Note) =>
            MathF.Abs(Note.JudgeTime - Game.Inst.GetGameTime()) switch
            {
                < NoteJudgeTime.CriticalPerfect => JudgeEnum.CriticalPerfect,
                < NoteJudgeTime.Perfect => JudgeEnum.Perfect,
                < NoteJudgeTime.Great => JudgeEnum.Great,
                < NoteJudgeTime.Good => JudgeEnum.Good,
                < NoteJudgeTime.Miss => JudgeEnum.Miss,
                > NoteJudgeTime.Miss => JudgeEnum.NotEntered,
                _ => throw new ArgumentOutOfRangeException(),
            };

        public static float GetJudgeScore(JudgeEnum Enum) =>
            Enum switch
            {
                JudgeEnum.CriticalPerfect => NoteJudgeScore.CriticalPerfect,
                JudgeEnum.Perfect => NoteJudgeScore.Perfect,
                JudgeEnum.Great => NoteJudgeScore.Great,
                JudgeEnum.Good => NoteJudgeScore.Good,
                JudgeEnum.Miss => NoteJudgeScore.Miss,
                JudgeEnum.NotEntered => NoteJudgeScore.Miss,
                _ => throw new ArgumentOutOfRangeException(),
            };
    }
}
