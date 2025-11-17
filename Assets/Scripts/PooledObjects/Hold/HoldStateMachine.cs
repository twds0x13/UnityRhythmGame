using Anime;
using HoldJudgeNS;
using HoldNS;
using InterpNS;
using StateMachine;
using UnityEngine;
using Game = GameManagerNS.GameManager;
using Judge = HoldJudgeNS.HoldJudge;

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

            this.AnimeMachine = Hold.AnimeMachine;
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
                StateMachine.SwitchState(Hold.OnJudge);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        // Hold 头部在进入判定时间时开始判定

        private bool StartJudge =>
            Game.Inst.GetGameTime() < Hold.JudgeTime
            && Judge.GetHeadJudgeEnum(Hold) != Judge.HoldJudgeEnum.NotEntered;
    }

    public class StateOnJudgeHold : HoldState
    {
        public StateOnJudgeHold(HoldBehaviour Hold, LinearStateMachine<HoldBehaviour> StateMachine)
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            RegisterJudge(); // 使用时注册
        }

        public override void Update()
        {
            base.Update();

            if (ExitJudge)
            {
                Hold.OnAutoMissed(); // 在没有判定 Hold 头部的情况下，超过判定时间之后自动 Miss 整条 Hold
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private bool ExitJudge =>
            Game.Inst.GetGameTime() > Hold.JudgeTime
            && Judge.GetHeadJudgeEnum(Hold) == Judge.HoldJudgeEnum.Miss;

        private void RegisterJudge()
        {
            Hold.ParentTrack.RegisterJudge(Hold);
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

            UnregisterJudge(); // 使用后注销
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void UnregisterJudge()
        {
            Hold.ParentTrack.UnregisterJudge(Hold);
        }
    }

    public class StatePressingJudgeHold : HoldState
    {
        public StatePressingJudgeHold(
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

            if (ExitJudge)
            {
                Hold.OnAutoFinish(); // 在已经判定过 Hold 头部的情况下，Hold 尾部在超过判定时间之后自动完成 Hold 判定
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private bool ExitJudge =>
            Game.Inst.GetGameTime() > Hold.JudgeTime + Hold.JudgeDuration
            && Judge.GetHeadJudgeEnum(Hold) == Judge.HoldJudgeEnum.NotEntered;
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
            Hold.SpriteRenderer.sprite = Hold.GetSprite("note_color");

            Hold.SetScale(Vector3.one * 0.110f);

            Hold.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);

            Hold.BodyAnimator.lineRenderer.material.color = new Color(1f, 1f, 1f, 1f);
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
                / AnimeMachine.CurAnime.TotalTimeElapse;

            Hold.transform.position =
                InterpFunc.VectorHandler(
                    AnimeMachine.CurAnime.StartV,
                    AnimeMachine.CurAnime.EndV,
                    AnimeMachine.CurT,
                    AxisFunc.Linear,
                    AxisFunc.Pow,
                    AxisFunc.Linear,
                    PowY: 1.25f
                ) * Hold.Vertical
                + Hold.ParentTrack.transform.position;
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

    public class StateJudgePressingAnimeHold : HoldState
    {
        public StateJudgePressingAnimeHold(
            HoldBehaviour Hold,
            LinearStateMachine<HoldBehaviour> StateMachine
        )
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            if (Judge.GetHeadJudgeEnum(Hold) == Judge.HoldJudgeEnum.CriticalPerfect) // 优化显示。Note 瞬移跳一下比较明显
            {
                AnimeMachine.JudgeCache = new Pack(
                    Hold.ParentTrack.transform.position,
                    Game.Inst.GetGameTime()
                );
            }
            else
            {
                AnimeMachine.JudgeCache = new Pack(
                    Hold.transform.position,
                    Game.Inst.GetGameTime()
                );
            }

            Hold.TailAnimator.AnimeMachine.JudgeCache = new Pack();
        }

        public override void Update()
        {
            base.Update();

            HoldAnime();
        }

        public override void Exit()
        {
            base.Exit();
        }

        public void HoldAnime()
        {
            Hold.transform.position = AnimeMachine.JudgePosCache;

            var elapsed = Mathf.Min(
                3f * (Game.Inst.GetGameTime() - AnimeMachine.JudgeTimeCache),
                1f
            );

            if (Hold.ParentTrack.TrackNumber < 2)
            {
                Hold.SpriteRenderer.color = new Color(
                    1f,
                    0.9f + 0.1f * elapsed,
                    0.9f + 0.1f * elapsed,
                    Mathf.Min(0.85f + 0.35f * elapsed, 1f)
                ); // 按住时的颜色
            }
            else
            {
                Hold.SpriteRenderer.color = new Color(
                    0.9f + 0.1f * elapsed,
                    0.9f + 0.1f * elapsed,
                    1f,
                    Mathf.Min(0.85f + 0.35f * elapsed, 1f)
                ); // 按住时的颜色
            }

            Hold.BodyAnimator.lineRenderer.material.color = Hold.SpriteRenderer.color;
        }
    }

    public class StateJudgeFinishAnimeHold : HoldState
    {
        public StateJudgeFinishAnimeHold(
            HoldBehaviour Hold,
            LinearStateMachine<HoldBehaviour> StateMachine
        )
            : base(Hold, StateMachine) { }

        public override void Enter()
        {
            base.Enter();

            AnimeMachine.JudgeCache = new Pack(Hold.transform.position, Game.Inst.GetGameTime());
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

            Hold.transform.position =
                new Vector3(0f, 0f * AnimeMachine.CurT, 0f) + AnimeMachine.JudgePosCache;

            if (AnimeMachine.HasJudgeAnime)
            {
                Hold.SpriteRenderer.color = new Color(
                    1f,
                    1f,
                    1f - 0.5f * AnimeMachine.CurT,
                    1f - 1f * AnimeMachine.CurT
                );

                Hold.BodyAnimator.lineRenderer.material.color = new Color(
                    1f,
                    1f,
                    1f - 0.5f * AnimeMachine.CurT,
                    Mathf.Max(0.5f - 0.75f * AnimeMachine.CurT, 0f)
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

            AnimeMachine.DisappearCache = new(Hold.transform.position, Game.Inst.GetGameTime());

            Hold.TailAnimator.AnimeMachine.DisappearCache = new(
                Hold.TailAnimator.transform.position,
                Game.Inst.GetGameTime()
            );
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

            Hold.transform.position =
                new Vector3(
                    0f,
                    -0.25f * ResizeDetector.Inst.Rect.rect.height * AnimeMachine.CurT,
                    0f
                ) + AnimeMachine.DisappearingPosCache;

            Hold.TailAnimator.transform.position =
                new Vector3(
                    0f,
                    -0.25f * ResizeDetector.Inst.Rect.rect.height * AnimeMachine.CurT,
                    0f
                ) + Hold.TailAnimator.AnimeMachine.DisappearingPosCache;

            Hold.SpriteRenderer.color = new Color(
                1f,
                1f,
                1f,
                Mathf.Max(0.6f - 0.9f * AnimeMachine.CurT, 0f)
            );

            Hold.BodyAnimator.lineRenderer.material.color = new Color(
                1f,
                1f,
                1f,
                Mathf.Max(0.8f - 1.2f * AnimeMachine.CurT, 0f)
            );

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
            Hold.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            Hold.transform.position = new Vector3(0f, 20f, 0f);
        }
    }
}
