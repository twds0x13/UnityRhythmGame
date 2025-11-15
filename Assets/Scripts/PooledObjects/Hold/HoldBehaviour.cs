using System.Collections.Generic;
using Anime;
using HoldJudgeNS;
using HoldStateMachine;
using PooledObjectNS;
using StateMachine;
using TrackNS;
using UnityEngine;
using Game = GameManagerNS.GameManager;
using Judge = HoldJudgeNS.HoldJudge;

namespace HoldNS
{
    public class HoldBehaviour : PooledObjectBehaviour, IChartObject, IVertical
    {
        private LinearStateMachine<HoldBehaviour> StateMachine; // 动画状态机

        public StateInitHold InitHold; // 从动画状态开始更新

        public StateAnimeHold AnimeHold; // 从动画状态开始更新

        public StateJudgePressingAnimeHold JudgePressingHoldAnime; // 被按住时的动画

        public StateJudgeFinishAnimeHold JudgeFinishHoldAnime; // 完成判定后消失的动画，是 Disappear 的分支

        public StateDisappearHold DisappearHoldAnime; // 如果没击中就处理消失动画逻辑

        public StateDestroyHold DestroyHoldAnime; // 击中或消失之后要删除 Hold

        private LinearStateMachine<HoldBehaviour> JudgeMachine; // 判定状态机

        public StateBeforeJudgeHold BeforeJudge; // 进入判定区之前

        public StateOnJudgeHold OnJudge; // 在判断区间内

        public StateAfterJudgeHold AfterJudge; // 过了判断区间

        public StatePressingJudgeHold PressingJudge; // 在判定区间内被按住

        public TrackBehaviour ParentTrack; // 归属的那一个轨道，获取 transform.position 作为动画坐标原点（默认情况落到轨道上）

        public HoldTailAnimator TailAnimator; // 对应尾部的动画管理器，只负责正确显示尾部和响应动画状态机对应的动画状态

        public HoldBodyAnimator BodyAnimator; // Hold 的身体部分动画管理器，负责根据头尾位置拉伸身体

        public float JudgeTime { get; private set; } // 预计判定时间

        public float JudgeDuration { get; private set; } // 预计按下时长

        public float Vertical { get; set; } = 1f; // 纵向位置缩放

        public void InitStateMachine(HoldBehaviour Hold)
        {
            StateMachine = new();

            InitHold = new(Hold, StateMachine);
            AnimeHold = new(Hold, StateMachine);
            JudgePressingHoldAnime = new(Hold, StateMachine);
            JudgeFinishHoldAnime = new(Hold, StateMachine);
            DisappearHoldAnime = new(Hold, StateMachine);
            DestroyHoldAnime = new(Hold, StateMachine);

            // 正常的状态跳转用的都是 SwitchState
            // 如果想从前面临时插一段进去就用 LinearStateMachine 的方法调用 NextState 函数
            // 通用状态机写起来太麻烦了 小修小补可以

            var AnimeList = new List<IState<HoldBehaviour>> { InitHold };

            JudgeMachine = new();
            BeforeJudge = new(Hold, JudgeMachine);
            OnJudge = new(Hold, JudgeMachine);
            AfterJudge = new(Hold, JudgeMachine);
            PressingJudge = new(Hold, JudgeMachine);

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
            if (JudgeMachine.CurState == OnJudge)
            {
                Game.Inst.Score.Score += Judge.GetJudgeScore(Judge.GetHeadJudgeEnum(this));

                Game.Inst.Score.MaxScore += HoldJudgeScore.Max;

                JudgeMachine.SwitchState(PressingJudge);
                StateMachine.SwitchState(JudgePressingHoldAnime);
            }
        }

        public void OnRelease()
        {
            if (JudgeMachine.CurState == PressingJudge) // 这个状态内置了如果没抬手就自动 OnAutoFinish 的功能（无尾判）
            {
                if (Judge.GetTailJudgeEnum(this) == Judge.HoldJudgeEnum.NotEntered) // 如果过早松开就不加分
                {
                    JudgeMachine.SwitchState(AfterJudge);
                    StateMachine.SwitchState(DisappearHoldAnime);
                }
                else // 只要不是过早松开就根据尾判开关加分
                {
                    Game.Inst.Score.Score += Judge.GetJudgeScore(Judge.GetTailJudgeEnum(this)); // TailJusgeEunm 可以开关是否拥有尾判

                    Game.Inst.Score.MaxScore += HoldJudgeScore.Max;

                    JudgeMachine.SwitchState(AfterJudge);
                    StateMachine.SwitchState(JudgeFinishHoldAnime);
                }
            }
        }

        public void OnAutoFinish() // 当长条已按下且进入尾判时间点时自动调用
        {
            Game.Inst.Score.Score += Judge.GetJudgeScore(Judge.GetTailJudgeEnum(this)); // TailJusgeEunm 可以开关是否拥有尾判

            Game.Inst.Score.MaxScore += HoldJudgeScore.Max;

            JudgeMachine.SwitchState(AfterJudge);
            StateMachine.SwitchState(JudgeFinishHoldAnime);
        }

        public void OnAutoMissed() // 当长条头部过了判定时间点（Miss）时自动调用
        {
            JudgeMachine.SwitchState(AfterJudge);
            StateMachine.SwitchState(DisappearHoldAnime);
        }

        public HoldBehaviour Init(
            AnimeMachine machine,
            TrackBehaviour track,
            float judgeTime,
            float holdDuration
        ) // 在 Objectpool 中调用这个函数作为通用起手，保证每次调用都从这里开始
        {
            JudgeTime = judgeTime;
            JudgeDuration = holdDuration;
            ParentTrack = track;
            AnimeMachine = machine;

            LogManager.Log(JudgeDuration.ToString());

            TailAnimator.Init(machine.ResetOffset(JudgeDuration), this);

            // BodyAnimator.Init(ParentTrack);

            InitStateMachine(this);
            return this;
        }

        public void ResetHold()
        {
            JudgeTime = 0f;
            JudgeDuration = 0f;

            AnimeMachine = null;

            Vertical = 1f;

            ParentTrack = null;

            StateMachine = null;
            JudgeMachine = null;
        }

        public override void OnClosePage()
        {
            if (StateMachine.CurState != JudgeFinishHoldAnime)
            {
                StateMachine.SwitchState(DisappearHoldAnime);
            }

            JudgeMachine.SwitchState(AfterJudge);
        }
    }
}
