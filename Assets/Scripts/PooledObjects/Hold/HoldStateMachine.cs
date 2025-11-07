using Anime;
using HoldJudgeNS;
using HoldNS;
using InterpNS;
using StateMachine;
using UnityEngine;
using Game = GameManagerNS.GameManager;
using Judge = HoldJudgeNS.HoldJudge;
using Page = UIManagerNS.PageManager;

namespace HoldStateMachine
{
    public class HoldState : IState<HoldBehaviour>
    {
        protected HoldBehaviour Hold;

        protected AnimeMachine AnimeMachine;

        protected LinearStateMachine<HoldBehaviour> StateMachine;

        public HoldState(HoldBehaviour Hold, LinearStateMachine<HoldBehaviour> StateMachine)
        {
            this.Hold = Hold;

            this.StateMachine = StateMachine;

            this.AnimeMachine = Hold.Inst.AnimeMachine;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }

    public class StateBeforeJudgeHold : HoldState
    {
        public StateBeforeJudgeHold(
            HoldBehaviour Hold,
            LinearStateMachine<HoldBehaviour> StateMachine
        )
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
        }

        public override void Update()
        {
            base.Update();

            if (StartJudge)
            {
                StateMachine.SwitchState(Hold.ProcessJudge);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private bool StartJudge =>
            Game.Inst.GetGameTime() < Hold.JudgeTime
            && Judge.GetJudgeEnum(Hold) != Judge.HoldJudgeEnum.NotEntered;
    }

    public class StateOnJudgeHold : HoldState
    {
        public StateOnJudgeHold(HoldBehaviour Hold, LinearStateMachine<HoldBehaviour> StateMachine)
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            RegisterJudge(); // 可以在使用的时候才注册，这样能省掉 JudgeList 里的额外判断！这是好文明
        }

        public override void Update()
        {
            base.Update();

            if (ExitJudge)
            {
                StateMachine.SwitchState(Hold.AfterJudge);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        // 在超过判定时间之后自动移除
        private bool ExitJudge =>
            Game.Inst.GetGameTime() > Hold.JudgeTime
            && Judge.GetJudgeEnum(Hold) == Judge.HoldJudgeEnum.Miss;

        private void RegisterJudge()
        {
            Hold.ParentTrack.RegisterJudge(Hold);
        }

        private void UnregisterJudge()
        {
            Hold.ParentTrack.UnregisterJudge(Hold);
        }
    }

    public class StateAfterJudgeHold : HoldState
    {
        public StateAfterJudgeHold(
            HoldBehaviour Hold,
            LinearStateMachine<HoldBehaviour> StateMachine
        )
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            Game.Inst.Score.MaxScore += HoldJudgeScore.Max;
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }
    }

    public class StateInitHold : HoldState
    {
        public StateInitHold(HoldBehaviour Hold, LinearStateMachine<HoldBehaviour> StateMachine)
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            CombineParent(Hold);

            AnimeInit(Hold);

            StateMachine.SwitchState(Hold.AnimeHold);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void CombineParent(HoldBehaviour Hold)
        {
            Hold.transform.SetParent(Hold.ParentTrack.transform, true);

            Hold.ParentTrack.Register(Hold);
        }

        private void AnimeInit(HoldBehaviour Hold)
        {
            Hold.Inst.SpriteRenderer.sprite = Hold.GetSprite(
                Hold.ParentTrack.TrackNumber < 1 || Hold.ParentTrack.TrackNumber > 2
                    ? "note_l"
                    : "note_color"
            );

            Hold.SetScale(Vector3.one);

            Hold.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public class StateAnimeHold : HoldState
    {
        public StateAnimeHold(HoldBehaviour Hold, LinearStateMachine<HoldBehaviour> StateMachine)
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
        }

        public override void Update()
        {
            base.Update();
            AnimeManager();
        }

        public override void Exit()
        {
            base.Exit();
        }

        public void UpdatePosition()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
                / AnimeMachine.CurAnime.TotalTimeElapse();

            Hold.transform.position =
                InterpFunc.VectorHandler(
                    AnimeMachine.CurAnime.StartV,
                    AnimeMachine.CurAnime.EndV,
                    AnimeMachine.CurT,
                    AxisFunc.Linear,
                    AxisFunc.Linear,
                    AxisFunc.Linear
                ) + Hold.ParentTrack.transform.position;
            ;
        }

        public void AnimeManager()
        {
            AnimeMachine.AnimeQueue.TryPeek(out AnimeMachine.CurAnime); // 至少 "应该" 有一个垫底动画

            if (Game.Inst.GetGameTime() < AnimeMachine.CurAnime.EndT)
            {
                UpdatePosition();
            }
            else
            {
                if (!AnimeMachine.AnimeQueue.TryDequeue(out AnimeMachine.CurAnime))
                {
                    if (AnimeMachine.HasDisappearAnime)
                    {
                        StateMachine.SwitchState(Hold.DisappearHoldAnime);
                    }
                    else
                    {
                        StateMachine.SwitchState(Hold.DestroyHoldAnime);
                    }
                }
            }
        }
    }

    public class StateJudgeAnimeHold : HoldState
    {
        public StateJudgeAnimeHold(
            HoldBehaviour Hold,
            LinearStateMachine<HoldBehaviour> StateMachine
        )
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            AnimeMachine.JudgeTimeCache = Game.Inst.GetGameTime();
            AnimeMachine.JudgePosCache = Hold.Inst.transform.position;
        }

        public override void Update()
        {
            base.Update();

            if (JudgeAnime())
            {
                StateMachine.SwitchState(Hold.DestroyHoldAnime);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        public bool JudgeAnime()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.JudgeTimeCache)
                / AnimeMachine.JudgeAnimeTimeSpan;

            Hold.Inst.transform.position =
                new Vector3(0f, 0f * AnimeMachine.CurT, 0f) + AnimeMachine.JudgePosCache;

            if (AnimeMachine.HasJudgeAnime)
            {
                Hold.Inst.SpriteRenderer.color = new Color(
                    1f,
                    1f,
                    1f - 2f * AnimeMachine.CurT,
                    1f - 2f * AnimeMachine.CurT
                );
            }
            else
            {
                StateMachine.SwitchState(Hold.DestroyHoldAnime);
            }

            return AnimeMachine.CurT - 1f >= 0;
        }
    }

    public class StateDisappearHold : HoldState
    {
        public StateDisappearHold(
            HoldBehaviour Hold,
            LinearStateMachine<HoldBehaviour> StateMachine
        )
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            AnimeMachine.DisappearTimeCache = Game.Inst.GetGameTime();
            AnimeMachine.DisappearingPosCache = Hold.Inst.transform.position;
        }

        public override void Update()
        {
            base.Update();

            if (Disappear())
            {
                StateMachine.SwitchState(Hold.DestroyHoldAnime);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        public bool Disappear()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.DisappearTimeCache)
                / AnimeMachine.DisappearTimeSpan;

            Hold.Inst.transform.position =
                new Vector3(
                    0f,
                    -0.25f * ResizeDetector.Inst.Rect.rect.height * AnimeMachine.CurT,
                    0f
                ) + AnimeMachine.DisappearingPosCache;

            Hold.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 0.3f - 0.3f * AnimeMachine.CurT);

            return AnimeMachine.CurT - 1f >= 0;
        }
    }

    public class StateDestroyHold : HoldState
    {
        public StateDestroyHold(HoldBehaviour Hold, LinearStateMachine<HoldBehaviour> StateMachine)
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            AnimeExit();

            ParentExit();

            Hold.DestroyEvent?.Invoke();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void ParentExit()
        {
            Hold.ParentTrack.Unregister(Hold);
            Hold.ParentTrack.UnregisterJudge(Hold);
        }

        private void AnimeExit()
        {
            Hold.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            Hold.Inst.transform.position = new Vector3(0f, 20f, 0f);
        }
    }
}
