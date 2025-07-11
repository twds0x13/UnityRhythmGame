using Anime;
using PooledObject;
using StateMachine;
using TrackManager;
using UnityEngine;
using Game = GameManager.GameManager;

namespace TrackStateMachine
{
    public class TrackState : IState<TrackBehaviour>
    {
        protected StateMachine<TrackBehaviour> StateMachine;

        protected TrackBehaviour Track;

        protected PooledObjectBehaviour Base;

        protected AnimeMachine AnimeMachine;

        public TrackState(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
        {
            this.Track = Track;
            this.StateMachine = StateMachine;

            Base = Track.GetBase();
            AnimeMachine = Base.Anime;
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
            Base.SpriteRenderer.sprite = Track.SpriteList[Base.Rand.Next(0, 2)];
            Base.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public class StateAnimeTrack : TrackState
    {
        public StateAnimeTrack(TrackBehaviour Track, StateMachine<TrackBehaviour> StateMachine)
            : base(Track, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Base.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
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
            Base.Anime.CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - Base.Anime.CurAnime.StartT)
                    / Base.Anime.CurAnime.TotalTimeElapse(),
                0.6f
            );

            Track.transform.position =
                (1 - Base.Anime.CurT) * Base.Anime.CurAnime.StartV
                + Base.Anime.CurT * Base.Anime.CurAnime.EndV;
        }

        public void AnimeManager()
        {
            Base.Anime.AnimeQueue.TryPeek(out Base.Anime.CurAnime); // 至少 "应该" 有一个垫底动画

            if (Game.Inst.GetGameTime() < Base.Anime.CurAnime.EndT)
            {
                UpdatePosition();
            }
            else
            {
                if (!Base.Anime.AnimeQueue.TryDequeue(out Base.Anime.CurAnime))
                {
                    if (Base.Anime.HasDisappearAnime)
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
            Base.Anime.DisappearTimeCache = Game.Inst.GetGameTime();
            Base.Anime.DisappearingPosCache = Base.transform.position;
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
            Base.Anime.CurT =
                (Game.Inst.GetGameTime() - Base.Anime.DisappearTimeCache)
                / Base.Anime.DisappearTimeSpan;

            Base.transform.position =
                new Vector3(0f, -Base.Anime.CurT, 0f) + Base.Anime.DisappearingPosCache;

            Base.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);

            return Base.Anime.CurT - 1f >= 0;
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
            Base.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f); // being a dirty hacker，but very trustful
            Base.transform.position = new Vector3(0f, 20f, 0f);
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
