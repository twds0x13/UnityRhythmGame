using Anime;
using InterpNS;
using StateMachine;
using TrackNS;
using Unity.VisualScripting;
using UnityEngine;
using Ctrl = GameCore.GameController;
using Game = GameManagerNS.GameManager;
using Page = UIManagerNS.PageController;

namespace TrackStateMachine
{
    public class TrackState : IState<TrackBehaviour>
    {
        protected TrackBehaviour Track;

        protected AnimeMachine AnimeMachine;

        protected StateMachine<TrackBehaviour> StateMachine;

        public TrackState(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
        {
            this.Track = Track;

            this.StateMachine = StateMachine;

            this.AnimeMachine = Track.Inst.AnimeMachine;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }

    public class StateInitJudgeTrack : TrackState
    {
        public StateInitJudgeTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            RegisterJudgeKey();
            StateMachine.SwitchState(Track.ProcessJudge);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void RegisterJudgeKey() // 神秘
        {
            if (Track.TrackNumber < 4)
            {
                Ctrl
                    .Inst.UserInput.currentActionMap.FindAction(
                        "Track " + Track.TrackNumber.ToString()
                    )
                    .performed += Track.JudgeNote;
            }
        }
    }

    public class StateProcessJudgeTrack : TrackState
    {
        public StateProcessJudgeTrack(
            TrackBehaviour Track,
            StateMachine<TrackBehaviour> StateMachine
        )
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
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

    public class StateFinishJudgeTrack : TrackState
    {
        public StateFinishJudgeTrack(
            TrackBehaviour Track,
            StateMachine<TrackBehaviour> StateMachine
        )
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            UnregisterJudgeKey();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void UnregisterJudgeKey()
        {
            if (Track.TrackNumber < 4)
            {
                Ctrl
                    .Inst.UserInput.currentActionMap.FindAction(
                        "Track " + Track.TrackNumber.ToString()
                    )
                    .performed -= Track.JudgeNote;
            }
        }
    }

    public class StateInitTrack : TrackState
    {
        public StateInitTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            InitTrack(Track);
            StateMachine.SwitchState(Track.AnimeTrack);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void InitTrack(TrackBehaviour Track)
        {
            Track.Inst.SpriteRenderer.sprite = Track.SpriteList[1];

            Track.transform.SetParent(Track.ParentPage.transform, false);

            Track.ParentPage.RegisterObject(Track);

            Track.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public class StateAnimeTrack : TrackState
    {
        public StateAnimeTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

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

        private void UpdatePosition()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
                / AnimeMachine.CurAnime.TotalTimeElapse();

            Track.transform.localPosition = InterpFunc.VectorHandler(
                AnimeMachine.CurAnime.StartV,
                AnimeMachine.CurAnime.EndV,
                AnimeMachine.CurT,
                AxisFunc.Sine,
                AxisFunc.Sine,
                AxisFunc.Sine
            );
        }

        private void AnimeManager()
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
                    if (!AnimeMachine.IsDestroyable) // 不可摧毁的track
                    {
                        return; // TODO : 注册全局广播接收器，在本局游戏退出时切换到 Disappear 状态（测试用 return 没问题）
                    }
                    if (AnimeMachine.HasDisappearAnime)
                    {
                        StateMachine.SwitchState(Track.DisappearTrack);
                    }
                    else
                    {
                        StateMachine.SwitchState(Track.DestroyTrack);
                    }
                }
            }
        }
    }

    public class StateDisappearTrack : TrackState
    {
        public StateDisappearTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            AnimeMachine.DisappearTimeCache = Game.Inst.GetGameTime();
            AnimeMachine.DisappearingPosCache = Track.Inst.transform.localPosition;
        }

        public override void Update()
        {
            base.Update();

            if (Disappear())
            {
                StateMachine.SwitchState(Track.DestroyTrack);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private bool Disappear()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.DisappearTimeCache)
                / AnimeMachine.DisappearTimeSpan;

            Track.Inst.transform.localPosition =
                new Vector3(0f, -0.25f * Page.Inst.GetPageRect().height * AnimeMachine.CurT, 0f)
                + AnimeMachine.DisappearingPosCache;

            Track.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f - AnimeMachine.CurT);

            return AnimeMachine.CurT - 1f >= 0;
        }
    }

    public class StateDestroyTrack : TrackState
    {
        public StateDestroyTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            AnimeExit();
            Track.DestroyEvent?.Invoke();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void AnimeExit()
        {
            Track.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            Track.Inst.transform.localPosition = new Vector3(0f, 20f, 0f);
        }
    }
}
