using Anime;
using NoteNamespace;
using PooledObject;
using StateMachine;
using UnityEngine;
using Game = GameManager.GameManager;

namespace NoteStateMachine
{
    public class NoteState : IState<NoteBehaviour>
    {
        protected NoteBehaviour Note;

        protected AnimeMachine AnimeMachine;

        protected StateMachine<NoteBehaviour> StateMachine;

        public NoteState(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
        {
            this.Note = Note;

            this.StateMachine = StateMachine;

            this.AnimeMachine = Note.Instance.AnimeMachine;
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
            Note.transform.SetParent(Note.ParentTrack.transform, true);
            Note.Instance.SpriteRenderer.sprite = Note.SpriteList[
                Note.Instance.RandInst.Next(0, 2)
            ];
            Note.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public class StateAnimeNote : NoteState
    {
        public StateAnimeNote(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Note.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
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
            AnimeMachine.CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - AnimeMachine.CurAnime.StartT)
                    / AnimeMachine.CurAnime.TotalTimeElapse(),
                0.45f
            );

            Note.transform.position =
                (1 - AnimeMachine.CurT) * AnimeMachine.CurAnime.StartV
                + AnimeMachine.CurT * AnimeMachine.CurAnime.EndV
                + Note.ParentTrack.CurPos();
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
            AnimeMachine.DisappearTimeCache = Game.Inst.GetGameTime();
            AnimeMachine.DisappearingPosCache = Note.Instance.transform.position;
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
            AnimeMachine.CurT =
                (Game.Inst.GetGameTime() - AnimeMachine.DisappearTimeCache)
                / AnimeMachine.DisappearTimeSpan;

            Note.Instance.transform.position =
                new Vector3(0f, -AnimeMachine.CurT, 0f) + AnimeMachine.DisappearingPosCache;

            Note.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 1f - AnimeMachine.CurT);

            return AnimeMachine.CurT - 1f >= 0;
        }
    }

    public class StateDestroyNote : NoteState
    {
        public StateDestroyNote(NoteBehaviour Note, StateMachine<NoteBehaviour> StateMachine)
            : base(Note, StateMachine) { }

        public override void Enter()
        {
            base.Enter();
            Note.Instance.SpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            Note.Instance.transform.position = new Vector3(0f, 20f, 0f);
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
