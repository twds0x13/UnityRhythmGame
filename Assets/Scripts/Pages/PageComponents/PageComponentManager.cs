using System;
using System.Collections.Generic;
using Singleton;
using Game = GameManagerNS.GameManager;
using Story = StoryNS.StoryManager;

namespace TextManagerNS
{
    public class PageComponentManager : Singleton<PageComponentManager>
    {
        public enum DynamicNum
        {
            GameTime,
            GameAbsTime,
            GameTimeScale,
            GameScore,
            GameMaxScore,
            GameAccuracy,
            StoryChapter,
            StoryParam,
            StoryLine,
        }

        public enum Template
        {
            MainTitle,
            SongSelection,
            SlideNums,
        }

        public string GetDynamicNumByType(DynamicNum Type)
        {
            switch (Type)
            {
                case DynamicNum.GameTime:
                    return Game.Inst.GetGameTime().ToString("F2");
                case DynamicNum.GameAbsTime:
                    return Game.Inst.GetAbsTime().ToString("F2");
                case DynamicNum.GameTimeScale:
                    return Game.Inst.GetTimeScale().ToString("F2");
                case DynamicNum.GameScore:
                    return Game.Inst.Score.Score.ToString("F2");
                case DynamicNum.GameMaxScore:
                    return Game.Inst.Score.MaxScore.ToString("F2");
                case DynamicNum.GameAccuracy:
                    return Game.Inst.Score.Accuracy.ToString("P2"); // "P" 代表百分数格式，其他同 "F"
                case DynamicNum.StoryChapter:
                    return 0f.ToString();
                //  return Story.Inst.StoryContainer.ToString();
            }
            throw new ArgumentOutOfRangeException("Text Type Out Of Range.");
        }

        public string[] GetDynamicNumsFromList(List<DynamicNum> List)
        {
            string[] strings = new string[List.Count];

            for (int i = 0; i < List.Count; i++)
            {
                strings[i] = GetDynamicNumByType(List[i]);
            }

            return strings;
        }

        protected override void SingletonAwake() { }
    }
}
