using Anime;
using NoteManager;
using PooledObject;
using StateMachine;
using UnityEngine;
using Game = GameManager.GameManager;

namespace NoteStateMachine
{
    public class NoteState : IState<NoteBehaviour>
    {
        protected StateMachine<NoteBehaviour> StateMachine;

        protected NoteBehaviour Note;

        protected PooledObjectBehaviour Base;

        protected AnimeMachine AnimeMachine;

        public NoteState(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
        {
            this.Note = Note;
            this.Base = Note.GetBase();
            this.AnimeMachine = Base.Anime;
            this.StateMachine = StateMachine;
        }

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }

    public class StateScoreNote : NoteState
    {
        public StateScoreNote(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

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

    public class StateInitNote : NoteState
    {
        public StateInitNote(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Enter Init");
            InitNote(Note);
            StateMachine.SwitchState(Note.AnimeNote);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Exit()
        {
            base.Exit();
        }

        public void InitNote(NoteBehaviour Note)
        {
            Note.isJudged = false;
            Base.SpriteRenderer.sprite = Note.SpriteList[Base.Rand.Next(0, 2)];
            Base.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public class StateAnimeNote : NoteState
    {
        public StateAnimeNote(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

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
        }

        public void UpdatePosition()
        {
            Base.Anime.CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - Base.Anime.CurAnime.StartT)
                    / Base.Anime.CurAnime.TotalTimeElapse(),
                0.6f
            );

            Note.transform.position =
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
                        StateMachine.SwitchState(Note.DisappearNote);
                    }
                    else
                    {
                        StateMachine.SwitchState(Note.DestroyNote);
                    }
                }
            }
        }
    }

    public class StateDisappearNote : NoteState
    {
        public StateDisappearNote(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

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
                StateMachine.SwitchState(Note.DestroyNote);
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        public bool Disappear()
        {
            Base.Anime.CurT =
                (Game.Inst.GetGameTime() - Base.Anime.DisappearTimeCache)
                / Base.Anime.DisappearTimeSpan;

            Base.transform.position =
                new Vector3(0f, -Base.Anime.CurT, 0f) + Base.Anime.DisappearingPosCache;

            Base.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f - Base.Anime.CurT);

            return Base.Anime.CurT - 1f >= 0;
        }
    }

    public class StateDestroyNote : NoteState
    {
        public StateDestroyNote(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Enter Destroy");
            Base.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f); // being a dirty hacker，but very trustful
            Base.transform.position = new Vector3(0f, 20f, 0f);
            Note.DestroyEvent?.Invoke();
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
