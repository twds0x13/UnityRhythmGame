using Anime;
using PooledObject;
using StateMachine;
using TrackNamespace;
using UnityEngine;
using Ctrl = GameCore.GameController;
using Game = GameManager.GameManager;

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

        private void RegisterJudgeKey() // 唉，硬编码
        {
            switch (Track.TrackNumber)
            {
                case 0:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track Zero").performed +=
                        Track.JudgeNote;
                    break;
                case 1:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track One").performed +=
                        Track.JudgeNote;
                    break;
                case 2:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track Two").performed +=
                        Track.JudgeNote;
                    break;
                case 3:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track Three").performed +=
                        Track.JudgeNote;
                    break;
            }

            Debug.Log("Track Succesfully Registered!");
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

            if (FinishJudge())
            {
                StateMachine.SwitchState(Track.FinishJudge);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private bool FinishJudge()
        {
            return false;
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
            switch (Track.TrackNumber)
            {
                case 0:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track Zero").performed -=
                        Track.JudgeNote;
                    break;
                case 1:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track One").performed -=
                        Track.JudgeNote;
                    break;
                case 2:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track Two").performed -=
                        Track.JudgeNote;
                    break;
                case 3:
                    Ctrl.Inst.NewInput.currentActionMap.FindAction("Track Three").performed -=
                        Track.JudgeNote;
                    break;
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
            AnimeMachine.CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
                    / AnimeMachine.CurAnime.TotalTimeElapse(),
                1f
            );

            Track.transform.position =
                (1 - AnimeMachine.CurT) * AnimeMachine.CurAnime.StartV
                + AnimeMachine.CurT * AnimeMachine.CurAnime.EndV;
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
            AnimeMachine.DisappearingPosCache = Track.Inst.transform.position;
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

            Track.Inst.transform.position =
                new Vector3(0f, -AnimeMachine.CurT, 0f) + AnimeMachine.DisappearingPosCache;

            Track.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);

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
            Track.Inst.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            Track.Inst.transform.position = new Vector3(0f, 20f, 0f);
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
    }
}
