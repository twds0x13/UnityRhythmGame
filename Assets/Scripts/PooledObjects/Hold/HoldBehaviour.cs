using System.Collections.Generic;
using Anime;
using HoldNS;
using HoldStateMachine;
using PooledObjectNS;
using StateMachine;
using TrackNS;
using UnityEngine;
using Game = GameManagerNS.GameManager;
using Judge = HoldJudgeNS.HoldJudge;

namespace HoldNS
{
    public class HoldBehaviour : PooledObjectBehaviour, IChartObject
    {
        private LinearStateMachine<HoldBehaviour> StateMachine; // 动画状态机

        public StateInitHold InitHold; // 从动画状态开始更新

        public StateAnimeHold AnimeHold; // 从动画状态开始更新

        public StateJudgeAnimeHold JudgeHoldAnime; // 播放被击中的动画，本质是 Disappear 的分支

        public StateDisappearHold DisappearHoldAnime; // 如果没击中就处理消失动画逻辑

        public StateDestroyHold DestroyHoldAnime; // 击中或消失之后要删除note

        private LinearStateMachine<HoldBehaviour> JudgeMachine; // 判定状态机

        public StateBeforeJudgeHold BeforeJudge; // 进入判定区之前

        public StateOnJudgeHold ProcessJudge; // 在判断区间内

        public StateAfterJudgeHold AfterJudge; // 过了判断区间

        public TrackBehaviour ParentTrack; // 归属的那一个轨道，获取 transform.position 作为动画坐标原点（默认情况落到轨道上）

        public float JudgeTime { get; private set; } // 预计判定时间

        public float HoldDuration { get; private set; } // 预计按下时长

        public void InitStateMachine(HoldBehaviour Hold)
        {
            StateMachine = new();

            InitHold = new(Hold, StateMachine);
            AnimeHold = new(Hold, StateMachine);
            JudgeHoldAnime = new(Hold, StateMachine);
            DisappearHoldAnime = new(Hold, StateMachine);
            DestroyHoldAnime = new(Hold, StateMachine);

            // 正常的状态跳转用的都是 SwitchState
            // 如果想从前面临时插一段进去就用 LinearStateMachine 的方法调用 NextState 函数
            // 通用状态机写起来太麻烦了 小修小补可以

            var AnimeList = new List<IState<HoldBehaviour>> { InitHold };

            JudgeMachine = new();
            BeforeJudge = new(Hold, JudgeMachine);
            ProcessJudge = new(Hold, JudgeMachine);
            AfterJudge = new(Hold, JudgeMachine);

            var JudgeList = new List<IState<HoldBehaviour>> { BeforeJudge };

            StateMachine.InitLinear(AnimeList);

            JudgeMachine.InitLinear(JudgeList);
        }

        private void Update() // 两个状态机
        {
            JudgeMachine.CurState?.Update();
            StateMachine.CurState?.Update();
        }

        public void OnPress()
        {
            if (JudgeMachine.CurState == ProcessJudge)
            {
                Game.Inst.Score.Score += Judge.GetJudgeScore(Judge.GetJudgeEnum(this));

                JudgeMachine.SwitchState(AfterJudge);
                StateMachine.SwitchState(JudgeHoldAnime);
            }
        }

        public void OnRelease() { }

        public HoldBehaviour Init(
            AnimeMachine Machine,
            TrackBehaviour Track,
            float judgeTime,
            float holdTime
        ) // 在 Objectpool 中调用这个函数作为通用起手，保证每次调用都从这里开始
        {
            JudgeTime = judgeTime;
            HoldDuration = holdTime;
            ParentTrack = Track;
            AnimeMachine = Machine;
            InitStateMachine(this);
            return this;
        }

        public override void OnClosePage()
        {
            if (StateMachine.CurState != JudgeHoldAnime)
            {
                StateMachine.SwitchState(DisappearHoldAnime);
            }

            JudgeMachine.SwitchState(AfterJudge);
        }
    }
}
