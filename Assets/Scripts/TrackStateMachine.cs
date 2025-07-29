using Anime;
using PooledObject;
using StateMachine;
using TrackNamespace;
using UnityEngine;
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

            this.AnimeMachine = Track.Instance.AnimeMachine;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }

    public class StateInitTrack : TrackState
    {
        public StateInitTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Enter Init");
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
            Debug.Log("Exit Init");
        }

        public void InitTrack(TrackBehaviour Track)
        {
            Track.Instance.SpriteRenderer.sprite = Track.SpriteList[
                Track.Instance.RandInst.Next(0, 2)
            ];
            Track.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public class StateAnimeTrack : TrackState
    {
        public StateAnimeTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Track.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            Debug.Log("Enter Anime");
        }

        public override void Update()
        {
            base.Update();
            AnimeManager();
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log("Exit Anime");
        }

        public void UpdatePosition()
        {
            AnimeMachine.CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
                    / AnimeMachine.CurAnime.TotalTimeElapse(),
                0.8f
            );

            Track.transform.position =
                (1 - AnimeMachine.CurT) * AnimeMachine.CurAnime.StartV
                + AnimeMachine.CurT * AnimeMachine.CurAnime.EndV;
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
                    if (!AnimeMachine.IsDestroyable) // 不可摧毁的track
                    {
                        return; // TODO : 注册全局广播接收器，在游戏退出时切换到 Disappear 状态（测试用 return 没问题）
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
            Debug.Log("Enter Disappear");
            AnimeMachine.DisappearTimeCache = Game.Inst.GetGameTime();
            AnimeMachine.DisappearingPosCache = Track.Instance.transform.position;
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
            Debug.Log("Exit Disappear");
        }

        public bool Disappear()
        {
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.DisappearTimeCache)
                / AnimeMachine.DisappearTimeSpan;

            Track.Instance.transform.position =
                new Vector3(0f, -AnimeMachine.CurT, 0f) + AnimeMachine.DisappearingPosCache;

            Track.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);

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
            Debug.Log("Enter Destroy");
            Track.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f); // being a dirty hacker，but very trustful
            Track.Instance.transform.position = new Vector3(0f, 20f, 0f);
            Track.DestroyEvent?.Invoke();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log("Exit Disappear");
        }
    }
}
